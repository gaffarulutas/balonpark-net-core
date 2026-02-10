using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Services;
using BalonPark.Models;

namespace BalonPark.Pages.Admin.Mails;

public class IndexModel : BaseAdminPage
{
    private readonly IMailService _mailService;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string Folder { get; set; } = "INBOX";

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public List<EmailMessage> Messages { get; set; } = [];
    public EmailStats Stats { get; set; } = new();
    public List<EmailFolder> Folders { get; set; } = [];

    public string FolderDisplayName => GetTurkishFolderName(Folder);

    public IndexModel(IMailService mailService, ILogger<IndexModel> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // Sayfa hızlı açılsın, tüm veriler AJAX ile yüklenecek
        // Server-side data loading tamamen kaldırıldı
        
        // Boş listelerle sayfa render et
        Messages = new List<EmailMessage>();
        Stats = new EmailStats();
        Folders = new List<EmailFolder>(); // Boş liste

        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(uint uid)
    {

        try
        {
            await _mailService.MarkAsReadAsync(Folder, uid);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostToggleFlagAsync(uint uid)
    {

        try
        {
            await _mailService.ToggleFlagAsync(Folder, uid);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(uint uid)
    {

        try
        {
            await _mailService.DeleteMessageAsync(Folder, uid);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostMoveToFolderAsync(uint uid, string targetFolder)
    {

        try
        {
            await _mailService.MoveToFolderAsync(Folder, uid, targetFolder);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
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
}

