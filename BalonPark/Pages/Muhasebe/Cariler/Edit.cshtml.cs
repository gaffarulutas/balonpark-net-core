using BalonPark.Data;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Cariler;

/// <summary>Cari düzenleme liste sayfasındaki modalda; bu route geriye dönük yönlendirme sağlar.</summary>
public class EditModel : BaseMuhasebePage
{
    public EditModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
    }

    public IActionResult OnGet(int id) => RedirectToPage("/Muhasebe/Cariler/Index", new { duzenle = id });
}
