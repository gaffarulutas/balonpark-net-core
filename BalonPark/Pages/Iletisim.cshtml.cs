using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Pages
{
    public class IletisimModel : BasePage
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<IletisimModel> _logger;

        [BindProperty]
        public ContactFormModel ContactForm { get; set; } = new();

        public bool IsEmailSent { get; set; } = false;
        public string? ErrorMessage { get; set; }

        public IletisimModel(
            CategoryRepository categoryRepository,
            SubCategoryRepository subCategoryRepository,
            SettingsRepository settingsRepository,
            IUrlService urlService, 
            IEmailService emailService,
            ILogger<IletisimModel> logger,
            ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public void OnGet()
        {
            // Contact page doesn't need any specific data loading
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var success = await _emailService.SendContactEmailAsync(ContactForm);
                
                if (success)
                {
                    IsEmailSent = true;
                    ContactForm = new ContactFormModel(); // Reset form
                    TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
                }
                else
                {
                    ErrorMessage = "Mesaj gönderilirken bir hata oluştu. Lütfen tekrar deneyiniz.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact form email");
                ErrorMessage = "Mesaj gönderilirken bir hata oluştu. Lütfen tekrar deneyiniz.";
            }

            return Page();
        }
    }
}
