using MimeKit;

namespace BalonPark.Models;

/// <summary>
/// Email mesajı model - IMAP/SMTP için
/// </summary>
public class EmailMessage
{
    public uint Uid { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public DateTime Date { get; set; }
    public bool IsSeen { get; set; }
    public bool IsFlagged { get; set; }
    public bool HasAttachments { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new();
    public string? InReplyTo { get; set; }
    public string? References { get; set; }
}

/// <summary>
/// Email eki model
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Email gönderme için model
/// </summary>
public class SendEmailModel
{
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment>? Attachments { get; set; }
    public string? InReplyTo { get; set; }
    public string? References { get; set; }
}

/// <summary>
/// Email istatistikleri
/// </summary>
public class EmailStats
{
    public int TotalInbox { get; set; }
    public int UnreadCount { get; set; }
    public int FlaggedCount { get; set; }
    public int SentCount { get; set; }
    public int DraftCount { get; set; }
    public int SpamCount { get; set; }
    public int TrashCount { get; set; }
}

/// <summary>
/// Email klasörleri
/// </summary>
public class EmailFolder
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int UnreadCount { get; set; }
    public EmailFolderType Type { get; set; }
}

public enum EmailFolderType
{
    Inbox,
    Sent,
    Drafts,
    Spam,
    Trash,
    Custom
}

