using BalonPark.Data;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Faturalar;

/// <summary>Yeni fatura liste sayfasındaki modalda.</summary>
public class CreateModel : BaseMuhasebePage
{
    public CreateModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
    }

    public IActionResult OnGet() => RedirectToPage("/Muhasebe/Faturalar/Index", new { yeni = true });

    public IActionResult OnPost() => RedirectToPage("/Muhasebe/Faturalar/Index", new { yeni = true });
}
