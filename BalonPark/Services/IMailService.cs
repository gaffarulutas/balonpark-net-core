using BalonPark.Models;

namespace BalonPark.Services;

/// <summary>
/// IMAP/SMTP tabanlı mail servisi interface
/// </summary>
public interface IMailService
{
    // IMAP - Email Okuma İşlemleri
    Task<List<EmailMessage>> GetInboxMessagesAsync(int page = 1, int pageSize = 20);
    Task<List<EmailMessage>> GetFolderMessagesAsync(string folderName, int page = 1, int pageSize = 20);
    Task<EmailMessage?> GetMessageByUidAsync(string folderName, uint uid);
    Task<EmailStats> GetEmailStatsAsync();
    Task<List<EmailFolder>> GetFoldersAsync();
    
    // Email İşaretleme
    Task<bool> MarkAsReadAsync(string folderName, uint uid);
    Task<bool> MarkAsUnreadAsync(string folderName, uint uid);
    Task<bool> ToggleFlagAsync(string folderName, uint uid);
    
    // Email Taşıma/Silme
    Task<bool> MoveToFolderAsync(string sourceFolderName, uint uid, string targetFolderName);
    Task<bool> DeleteMessageAsync(string folderName, uint uid);
    
    // SMTP - Email Gönderme
    Task<bool> SendEmailAsync(SendEmailModel model);
    Task<bool> ReplyToEmailAsync(EmailMessage originalMessage, string replyBody, bool isHtml = true);
    
    // Arama
    Task<List<EmailMessage>> SearchMessagesAsync(string query, string folderName = "INBOX");
}

