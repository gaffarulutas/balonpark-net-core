using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Mails;

public class ViewModel : BaseAdminPage
{
    private readonly IMailService _mailService;

    [BindProperty(SupportsGet = true)]
    public string Folder { get; set; } = "INBOX";

    [BindProperty(SupportsGet = true)]
    public uint Uid { get; set; }

    public EmailMessage? Message { get; set; }

    public ViewModel(IMailService mailService)
    {
        _mailService = mailService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Message = await _mailService.GetMessageByUidAsync(Folder, Uid);
            
            if (Message == null)
            {
                TempData["ErrorMessage"] = "Mesaj bulunamadı.";
                return RedirectToPage("/Admin/Mails/Index");
            }

            // Okunmadıysa okundu olarak işaretle (arka planda)
            if (!Message.IsSeen)
            {
                // Async olarak işaretle, bekleme
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _mailService.MarkAsReadAsync(Folder, Uid);
                    }
                    catch { /* Ignore errors */ }
                });
                Message.IsSeen = true;
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Mesaj yüklenirken hata oluştu: {ex.Message}";
            return RedirectToPage("/Admin/Mails/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleFlagAsync()
    {

        try
        {
            await _mailService.ToggleFlagAsync(Folder, Uid);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {

        try
        {
            await _mailService.DeleteMessageAsync(Folder, Uid);
            return new JsonResult(new { success = true, redirectUrl = $"/admin/mails?folder={Folder}" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostMoveToFolderAsync(string targetFolder)
    {

        try
        {
            await _mailService.MoveToFolderAsync(Folder, Uid, targetFolder);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}

