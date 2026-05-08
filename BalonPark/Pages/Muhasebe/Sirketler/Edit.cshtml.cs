using BalonPark.Data;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Sirketler;

/// <summary>Şirket düzenleme liste sayfasındaki modalda.</summary>
public class EditModel : BaseMuhasebePage
{
    public EditModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
    }

    protected override bool RequiresSelectedCompany => false;

    public IActionResult OnGet(int id) => RedirectToPage("/Muhasebe/Sirketler/Index", new { duzenle = id });
}
