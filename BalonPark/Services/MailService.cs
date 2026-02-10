using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using BalonPark.Models;
using System.Collections.Concurrent;

namespace BalonPark.Services;

/// <summary>
/// IMAP/SMTP tabanlı mail servisi implementasyonu
/// Best practices: Connection pooling, retry logic, proper disposal
/// </summary>
public class MailService(IConfiguration configuration, ILogger<MailService> logger) : IMailService, IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private ImapClient? _sharedImapClient;
    private DateTime _lastConnectionTime = DateTime.MinValue;
    private readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(5);
    private bool _disposed = false;

    #region IMAP - Email Okuma İşlemleri

    public async Task<List<EmailMessage>> GetInboxMessagesAsync(int page = 1, int pageSize = 20)
    {
        return await GetFolderMessagesAsync("INBOX", page, pageSize);
    }

    public async Task<List<EmailMessage>> GetFolderMessagesAsync(string folderName, int page = 1, int pageSize = 20)
    {
        var messages = new List<EmailMessage>();
        ImapClient? client = null;
        
        try
        {
            // Her istek için yeni client oluştur (thread safety için)
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            if (folder.Count == 0)
                return messages;

            // Tüm UID'leri al (en güvenli yöntem)
            var uids = await folder.SearchAsync(SearchQuery.All);
            if (uids.Count == 0)
                return messages;

            // Sayfalama için UID'leri sırala ve filtrele (en yeni önce)
            var sortedUids = uids.OrderByDescending(u => u.Id).ToList();
            var skip = (page - 1) * pageSize;
            var pagedUids = sortedUids.Skip(skip).Take(pageSize).ToList();

            if (!pagedUids.Any())
                return messages;

            // Mesajları UID ile fetch et
            foreach (var uid in pagedUids)
            {
                try
                {
                    var message = await folder.GetMessageAsync(uid);
                    var summaries = await folder.FetchAsync(new[] { uid }, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure);
                    var summary = summaries.FirstOrDefault();
                    
                    if (summary != null)
                    {
                        messages.Add(ConvertToEmailMessage(message, summary));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Skipping message {uid} from {folderName} - may have been deleted");
                    continue;
                }
            }

            await folder.CloseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error getting messages from {folderName}");
        }
        finally
        {
            // Client'ı kapat ve dispose et (thread safety için her istekte yeni client)
            if (client != null)
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    client.Dispose();
                }
            }
        }

        return messages;
    }

    /// <summary>
    /// Bağlantıyı sıfırlar
    /// </summary>
    private async Task ResetConnectionAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_sharedImapClient != null)
            {
                try
                {
                    if (_sharedImapClient.IsConnected)
                        await _sharedImapClient.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    _sharedImapClient.Dispose();
                    _sharedImapClient = null;
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<EmailMessage?> GetMessageByUidAsync(string folderName, uint uid)
    {
        ImapClient? client = null;
        
        try
        {
            // Her istek için yeni client oluştur (thread safety için)
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            var uniqueId = new UniqueId(uid);
            var message = await folder.GetMessageAsync(uniqueId);
            var summaries = await folder.FetchAsync(new[] { uniqueId }, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure);
            var summary = summaries.FirstOrDefault();

            await folder.CloseAsync();

            if (summary == null)
                return null;

            return ConvertToEmailMessage(message, summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error getting message {uid} from {folderName}");
            return null;
        }
        finally
        {
            // Client'ı kapat ve dispose et
            if (client != null)
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    client.Dispose();
                }
            }
        }
    }

    public async Task<EmailStats> GetEmailStatsAsync()
    {
        var stats = new EmailStats();
        ImapClient? client = null;
        
        try
        {
            // Her istek için yeni client oluştur (thread safety için)
            client = await CreateImapClientWithRetryAsync();

            // Inbox istatistikleri
            var inbox = await client.GetFolderAsync("INBOX");
            await inbox.OpenAsync(FolderAccess.ReadOnly);
            stats.TotalInbox = inbox.Count;
            
            var unreadQuery = SearchQuery.NotSeen;
            var unreadUids = await inbox.SearchAsync(unreadQuery);
            stats.UnreadCount = unreadUids.Count;

            var flaggedQuery = SearchQuery.Flagged;
            var flaggedUids = await inbox.SearchAsync(flaggedQuery);
            stats.FlaggedCount = flaggedUids.Count;

            await inbox.CloseAsync();

            // Sent istatistikleri
            try
            {
                var sent = await client.GetFolderAsync("Sent");
                await sent.OpenAsync(FolderAccess.ReadOnly);
                stats.SentCount = sent.Count;
                await sent.CloseAsync();
            }
            catch { stats.SentCount = 0; }

            // Drafts istatistikleri
            try
            {
                var drafts = await client.GetFolderAsync("Drafts");
                await drafts.OpenAsync(FolderAccess.ReadOnly);
                stats.DraftCount = drafts.Count;
                await drafts.CloseAsync();
            }
            catch { stats.DraftCount = 0; }

            // Spam istatistikleri
            try
            {
                var spam = await client.GetFolderAsync("Spam");
                await spam.OpenAsync(FolderAccess.ReadOnly);
                stats.SpamCount = spam.Count;
                await spam.CloseAsync();
            }
            catch 
            { 
                try
                {
                    var junk = await client.GetFolderAsync("Junk");
                    await junk.OpenAsync(FolderAccess.ReadOnly);
                    stats.SpamCount = junk.Count;
                    await junk.CloseAsync();
                }
                catch { stats.SpamCount = 0; }
            }

            // Trash istatistikleri
            try
            {
                var trash = await client.GetFolderAsync("Trash");
                await trash.OpenAsync(FolderAccess.ReadOnly);
                stats.TrashCount = trash.Count;
                await trash.CloseAsync();
            }
            catch { stats.TrashCount = 0; }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-posta istatistikleri alınırken hata");
        }
        finally
        {
            // Client'ı kapat ve dispose et
            if (client != null)
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    client.Dispose();
                }
            }
        }

        return stats;
    }

    public async Task<List<EmailFolder>> GetFoldersAsync()
    {
        var folders = new List<EmailFolder>();
        ImapClient? client = null;
        
        try
        {
            // Her istek için yeni client oluştur (thread safety için)
            client = await CreateImapClientWithRetryAsync();
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            var subfolders = await personal.GetSubfoldersAsync();

            foreach (var folder in subfolders)
            {
                try
                {
                    await folder.OpenAsync(FolderAccess.ReadOnly);
                    
                    var unreadQuery = SearchQuery.NotSeen;
                    var unreadUids = await folder.SearchAsync(unreadQuery);

                    folders.Add(new EmailFolder
                    {
                        Name = folder.FullName,
                        DisplayName = GetTurkishFolderName(folder.Name),
                        MessageCount = folder.Count,
                        UnreadCount = unreadUids.Count,
                        Type = GetFolderType(folder.Name)
                    });

                    await folder.CloseAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Could not access folder {folder.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Klasörler alınırken hata");
        }
        finally
        {
            // Client'ı kapat ve dispose et
            if (client != null)
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    client.Dispose();
                }
            }
        }

        return folders;
    }

    #endregion

    #region Email İşaretleme

    public async Task<bool> MarkAsReadAsync(string folderName, uint uid)
    {
        ImapClient? client = null;
        
        try
        {
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var uniqueId = new UniqueId(uid);
            await folder.AddFlagsAsync(uniqueId, MessageFlags.Seen, true);

            await folder.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error marking message {uid} as read in {folderName}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                try { if (client.IsConnected) await client.DisconnectAsync(true); }
                catch { }
                finally { client.Dispose(); }
            }
        }
    }

    public async Task<bool> MarkAsUnreadAsync(string folderName, uint uid)
    {
        ImapClient? client = null;
        
        try
        {
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var uniqueId = new UniqueId(uid);
            await folder.RemoveFlagsAsync(uniqueId, MessageFlags.Seen, true);

            await folder.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error marking message {uid} as unread in {folderName}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                try { if (client.IsConnected) await client.DisconnectAsync(true); }
                catch { }
                finally { client.Dispose(); }
            }
        }
    }

    public async Task<bool> ToggleFlagAsync(string folderName, uint uid)
    {
        ImapClient? client = null;
        
        try
        {
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var uniqueId = new UniqueId(uid);
            var summaries = await folder.FetchAsync(new[] { uniqueId }, MessageSummaryItems.Flags);
            var summary = summaries.FirstOrDefault();

            if (summary != null && summary.Flags.HasValue && summary.Flags.Value.HasFlag(MessageFlags.Flagged))
            {
                await folder.RemoveFlagsAsync(uniqueId, MessageFlags.Flagged, true);
            }
            else
            {
                await folder.AddFlagsAsync(uniqueId, MessageFlags.Flagged, true);
            }

            await folder.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error toggling flag for message {uid} in {folderName}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                try { if (client.IsConnected) await client.DisconnectAsync(true); }
                catch { }
                finally { client.Dispose(); }
            }
        }
    }

    #endregion

    #region Email Taşıma/Silme

    public async Task<bool> MoveToFolderAsync(string sourceFolderName, uint uid, string targetFolderName)
    {
        ImapClient? client = null;
        
        try
        {
            client = await CreateImapClientWithRetryAsync();
            var sourceFolder = await client.GetFolderAsync(sourceFolderName);
            await sourceFolder.OpenAsync(FolderAccess.ReadWrite);

            var targetFolder = await client.GetFolderAsync(targetFolderName);

            var uniqueId = new UniqueId(uid);
            await sourceFolder.MoveToAsync(uniqueId, targetFolder);

            await sourceFolder.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error moving message {uid} from {sourceFolderName} to {targetFolderName}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                try { if (client.IsConnected) await client.DisconnectAsync(true); }
                catch { }
                finally { client.Dispose(); }
            }
        }
    }

    public async Task<bool> DeleteMessageAsync(string folderName, uint uid)
    {
        ImapClient? client = null;
        
        try
        {
            client = await CreateImapClientWithRetryAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadWrite);

            var uniqueId = new UniqueId(uid);
            
            // Trash klasörüne taşı veya kalıcı olarak sil
            try
            {
                var trash = await client.GetFolderAsync("Trash");
                await folder.MoveToAsync(uniqueId, trash);
            }
            catch
            {
                // Trash yoksa deleted flag ekle
                await folder.AddFlagsAsync(uniqueId, MessageFlags.Deleted, true);
                await folder.ExpungeAsync();
            }

            await folder.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error deleting message {uid} from {folderName}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                try { if (client.IsConnected) await client.DisconnectAsync(true); }
                catch { }
                finally { client.Dispose(); }
            }
        }
    }

    #endregion

    #region SMTP - Email Gönderme

    public async Task<bool> SendEmailAsync(SendEmailModel model)
    {
        Exception? lastException = null;
        
        // Retry logic - email gönderimi için 2 deneme
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var message = new MimeMessage();
                var emailSettings = configuration.GetSection("EmailSettings");
                
                message.From.Add(new MailboxAddress(
                    emailSettings["FromName"] ?? "Balon Park",
                    emailSettings["FromEmail"] ?? "info@balonpark.com"
                ));
                
                message.To.Add(new MailboxAddress(model.ToName, model.To));
                message.Subject = model.Subject;

                var builder = new BodyBuilder();
                if (model.IsHtml)
                {
                    builder.HtmlBody = model.Body;
                }
                else
                {
                    builder.TextBody = model.Body;
                }

                // Ekler varsa ekle
                if (model.Attachments != null && model.Attachments.Any())
                {
                    foreach (var attachment in model.Attachments)
                    {
                        builder.Attachments.Add(attachment.FileName, attachment.Data);
                    }
                }

                // Reply bilgilerini ekle
                if (!string.IsNullOrEmpty(model.InReplyTo))
                {
                    message.InReplyTo = model.InReplyTo;
                }

                if (!string.IsNullOrEmpty(model.References))
                {
                    message.References.Add(model.References);
                }

                message.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient
                {
                    Timeout = 30000,
                    ServerCertificateValidationCallback = (s, c, h, e) => true
                };

                logger.LogInformation($"SMTP connection attempt {attempt} to {emailSettings["SmtpServer"]}");

                await smtp.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    emailSettings["SmtpUsername"],
                    emailSettings["SmtpPassword"]
                );

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                logger.LogInformation("Email sent successfully to {To}", model.To);
                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(ex, $"SMTP send attempt {attempt} failed for {model.To}");
                
                if (attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2)); // 2 saniye bekle
                }
            }
        }

        logger.LogError(lastException, "Failed to send email to {To} after 2 attempts", model.To);
        return false;
    }

    public async Task<bool> ReplyToEmailAsync(EmailMessage originalMessage, string replyBody, bool isHtml = true)
    {
        var model = new SendEmailModel
        {
            To = originalMessage.From,
            ToName = originalMessage.FromName,
            Subject = originalMessage.Subject.StartsWith("RE:") 
                ? originalMessage.Subject 
                : $"RE: {originalMessage.Subject}",
            Body = replyBody,
            IsHtml = isHtml,
            InReplyTo = originalMessage.MessageId,
            References = originalMessage.MessageId
        };

        return await SendEmailAsync(model);
    }

    #endregion

    #region Arama

    public async Task<List<EmailMessage>> SearchMessagesAsync(string query, string folderName = "INBOX")
    {
        var messages = new List<EmailMessage>();

        try
        {
            var client = await GetImapClientAsync();
            var folder = await client.GetFolderAsync(folderName);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            // Konu veya gövdede ara
            var searchQuery = SearchQuery.SubjectContains(query)
                .Or(SearchQuery.BodyContains(query))
                .Or(SearchQuery.FromContains(query));

            var uids = await folder.SearchAsync(searchQuery);

            foreach (var uid in uids.Take(50)) // İlk 50 sonuç
            {
                try
                {
                    var message = await folder.GetMessageAsync(uid);
                    var summaries = await folder.FetchAsync(new[] { uid }, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure);
                    var summary = summaries.FirstOrDefault();
                    
                    if (summary != null)
                        messages.Add(ConvertToEmailMessage(message, summary));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error fetching search result {uid}");
                }
            }

            await folder.CloseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error searching messages in {folderName}");
            
            if (ex is System.Net.Sockets.SocketException)
                await ResetConnectionAsync();
        }

        return messages;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// IMAP Client bağlantısını yönetir - Connection pooling ile
    /// </summary>
    private async Task<ImapClient> GetImapClientAsync()
    {
        await _connectionLock.WaitAsync();
        
        try
        {
            // Mevcut bağlantı hala geçerliyse onu kullan
            if (_sharedImapClient != null && 
                _sharedImapClient.IsConnected && 
                _sharedImapClient.IsAuthenticated &&
                DateTime.UtcNow - _lastConnectionTime < _connectionTimeout)
            {
                logger.LogDebug("Reusing existing IMAP connection");
                return _sharedImapClient;
            }

            // Eski bağlantıyı temizle
            if (_sharedImapClient != null)
            {
                try
                {
                    if (_sharedImapClient.IsConnected)
                        await _sharedImapClient.DisconnectAsync(true);
                }
                catch { }
                finally
                {
                    _sharedImapClient.Dispose();
                    _sharedImapClient = null;
                }
            }

            // Yeni bağlantı oluştur - Retry logic ile
            _sharedImapClient = await CreateImapClientWithRetryAsync();
            _lastConnectionTime = DateTime.UtcNow;
            
            return _sharedImapClient;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Retry logic ile IMAP client oluşturur
    /// </summary>
    private async Task<ImapClient> CreateImapClientWithRetryAsync(int maxRetries = 3)
    {
        var emailSettings = configuration.GetSection("EmailSettings");
        var server = emailSettings["ImapServer"];
        var port = int.Parse(emailSettings["ImapPort"] ?? "993");
        var username = emailSettings["ImapUsername"] ?? emailSettings["SmtpUsername"];
        var password = emailSettings["ImapPassword"] ?? emailSettings["SmtpPassword"];

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var client = new ImapClient
            {
                Timeout = 30000, // 30 saniye timeout
                ServerCertificateValidationCallback = (s, c, h, e) => true // Geliştirme için - Production'da kaldırın
            };

            try
            {
                logger.LogInformation($"IMAP connection attempt {attempt}/{maxRetries} to {server}:{port}");

                var secureOption = port == 993 
                    ? SecureSocketOptions.SslOnConnect 
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(server, port, secureOption);
                await client.AuthenticateAsync(username, password);

                logger.LogInformation("IMAP connection successful");
                return client;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(ex, $"IMAP connection attempt {attempt} failed");

                client.Dispose();

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    logger.LogInformation($"Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay);
                }
            }
        }

        logger.LogError(lastException, "Failed to connect to IMAP server after {MaxRetries} attempts", maxRetries);
        throw new Exception($"IMAP sunucusuna {maxRetries} denemeden sonra bağlanılamadı. Sunucu ayarlarını kontrol edin.", lastException);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connectionLock?.Dispose();
                
                if (_sharedImapClient != null)
                {
                    try
                    {
                        if (_sharedImapClient.IsConnected)
                            _sharedImapClient.Disconnect(true);
                    }
                    catch { }
                    finally
                    {
                        _sharedImapClient.Dispose();
                    }
                }
            }
            _disposed = true;
        }
    }

    private EmailMessage ConvertToEmailMessage(MimeMessage message, IMessageSummary summary)
    {
        var emailMessage = new EmailMessage
        {
            Uid = summary.UniqueId.Id,
            MessageId = message.MessageId,
            Subject = message.Subject ?? "(Konu yok)",
            Date = message.Date.DateTime,
            IsSeen = summary.Flags.HasValue && summary.Flags.Value.HasFlag(MessageFlags.Seen),
            IsFlagged = summary.Flags.HasValue && summary.Flags.Value.HasFlag(MessageFlags.Flagged),
            InReplyTo = message.InReplyTo,
            References = message.References.FirstOrDefault()
        };

        // Gönderen bilgisi
        if (message.From.Mailboxes.Any())
        {
            var from = message.From.Mailboxes.First();
            emailMessage.From = from.Address;
            emailMessage.FromName = from.Name ?? from.Address;
        }

        // Alıcı bilgisi
        if (message.To.Mailboxes.Any())
        {
            var to = message.To.Mailboxes.First();
            emailMessage.To = to.Address;
        }

        // Gövde
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            emailMessage.Body = message.HtmlBody;
            emailMessage.IsHtml = true;
        }
        else if (!string.IsNullOrEmpty(message.TextBody))
        {
            emailMessage.Body = message.TextBody;
            emailMessage.IsHtml = false;
        }

        // Ekler
        foreach (var attachment in message.Attachments)
        {
            if (attachment is MimePart mimePart)
            {
                using var memory = new MemoryStream();
                mimePart.Content.DecodeTo(memory);
                
                emailMessage.Attachments.Add(new EmailAttachment
                {
                    FileName = mimePart.FileName ?? "unknown",
                    ContentType = mimePart.ContentType.MimeType,
                    Size = memory.Length,
                    Data = memory.ToArray()
                });
            }
        }

        emailMessage.HasAttachments = emailMessage.Attachments.Any();

        return emailMessage;
    }

    private EmailFolderType GetFolderType(string folderName)
    {
        var lowerName = folderName.ToLowerInvariant();
        
        // Inbox variants
        if (lowerName == "inbox" || lowerName == "gelen kutusu")
            return EmailFolderType.Inbox;
        
        // Sent variants
        if (lowerName.Contains("sent") || lowerName.Contains("gönder") || 
            lowerName == "sent items" || lowerName == "sent mail" || 
            lowerName == "sent messages" || lowerName == "gönderilmiş öğeler")
            return EmailFolderType.Sent;
        
        // Drafts variants
        if (lowerName.Contains("draft") || lowerName.Contains("taslak"))
            return EmailFolderType.Drafts;
        
        // Spam variants
        if (lowerName.Contains("spam") || lowerName.Contains("junk") || 
            lowerName.Contains("gereksiz") || lowerName.Contains("önemsiz"))
            return EmailFolderType.Spam;
        
        // Trash variants
        if (lowerName.Contains("trash") || lowerName.Contains("deleted") || 
            lowerName.Contains("çöp") || lowerName.Contains("silinmiş"))
            return EmailFolderType.Trash;
        
        return EmailFolderType.Custom;
    }

    private string GetTurkishFolderName(string folderName)
    {
        var lowerName = folderName.ToLowerInvariant();
        
        // Inbox variants
        if (lowerName == "inbox" || lowerName == "gelen kutusu")
            return "Gelen Kutusu";
        
        // Sent variants
        if (lowerName.Contains("sent") || lowerName.Contains("gönder") || 
            lowerName == "sent items" || lowerName == "sent mail" || 
            lowerName == "sent messages" || lowerName == "gönderilmiş öğeler")
            return "Gönderilenler";
        
        // Drafts variants
        if (lowerName.Contains("draft") || lowerName.Contains("taslak"))
            return "Taslaklar";
        
        // Spam variants
        if (lowerName.Contains("spam") || lowerName.Contains("junk") || 
            lowerName.Contains("gereksiz") || lowerName.Contains("önemsiz"))
            return "Gereksiz";
        
        // Trash variants
        if (lowerName.Contains("trash") || lowerName.Contains("deleted") || 
            lowerName.Contains("çöp") || lowerName.Contains("silinmiş"))
            return "Çöp Kutusu";
        
        // Özel klasörler için orijinal ismi kullan
        return folderName;
    }

    #endregion
}

