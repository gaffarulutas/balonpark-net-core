using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Mails;

public class ComposeModel : BaseAdminPage
{
    private readonly IMailService _mailService;

    [BindProperty]
    public SendEmailModel EmailModel { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReplyTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Folder { get; set; }

    [BindProperty(SupportsGet = true)]
    public uint? Uid { get; set; }

    public EmailMessage? OriginalMessage { get; set; }

    public ComposeModel(IMailService mailService)
    {
        _mailService = mailService;
    }

    public async Task<IActionResult> OnGetAsync()
    {

        // Yanıt modu ise orijinal mesajı yükle
        if (!string.IsNullOrEmpty(Folder) && Uid.HasValue)
        {
            OriginalMessage = await _mailService.GetMessageByUidAsync(Folder, Uid.Value);
            if (OriginalMessage != null)
            {
                EmailModel.To = OriginalMessage.From;
                EmailModel.ToName = OriginalMessage.FromName;
                EmailModel.Subject = OriginalMessage.Subject.StartsWith("RE:") 
                    ? OriginalMessage.Subject 
                    : $"RE: {OriginalMessage.Subject}";
                EmailModel.InReplyTo = OriginalMessage.MessageId;
                EmailModel.References = OriginalMessage.MessageId;
            }
        }
        else if (!string.IsNullOrEmpty(ReplyTo))
        {
            EmailModel.To = ReplyTo;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var success = await _mailService.SendEmailAsync(EmailModel);

            if (success)
            {
                TempData["SuccessMessage"] = "Email başarıyla gönderildi.";
                return RedirectToPage("/Admin/Mails/Index", new { Folder = "Sent" });
            }
            else
            {
                TempData["ErrorMessage"] = "Email gönderilirken bir hata oluştu.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
            return Page();
        }
    }
}

