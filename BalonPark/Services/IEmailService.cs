using System.ComponentModel.DataAnnotations;

namespace BalonPark.Services;

public interface IEmailService
{
    Task<bool> SendContactEmailAsync(ContactFormModel contactForm);
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
}

public class ContactFormModel
{
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu alanı zorunludur.")]
    [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
    [Display(Name = "Konu")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
    [StringLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
    [Display(Name = "Mesaj")]
    public string Message { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.Now;
}
