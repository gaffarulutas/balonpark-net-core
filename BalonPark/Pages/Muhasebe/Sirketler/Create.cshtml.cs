using BalonPark.Data;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Sirketler;

/// <summary>Yeni şirket artık liste sayfasındaki modalda.</summary>
public class CreateModel : BaseMuhasebePage
{
    public CreateModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
    }

    protected override bool RequiresSelectedCompany => false;

    public IActionResult OnGet() => RedirectToPage("/Muhasebe/Sirketler/Index", new { yeni = true });

    public IActionResult OnPost() => RedirectToPage("/Muhasebe/Sirketler/Index", new { yeni = true });
}
