using BalonPark.Data;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Cariler;

/// <summary>Yeni cari artık liste sayfasındaki modalda; bu route geriye dönük yönlendirme sağlar.</summary>
public class CreateModel : BaseMuhasebePage
{
    public CreateModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
    }

    public IActionResult OnGet() => RedirectToPage("/Muhasebe/Cariler/Index");

    public IActionResult OnPost() => RedirectToPage("/Muhasebe/Cariler/Index");
}
