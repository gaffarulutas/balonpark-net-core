using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe;

public class SirketSecModel : BaseMuhasebePage
{
    private readonly AccountingCompanyRepository _repository;

    public SirketSecModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
        _repository = accountingCompanyRepository;
    }

    protected override bool RequiresSelectedCompany => false;

    public IReadOnlyList<AccountingCompany> Companies { get; private set; } = Array.Empty<AccountingCompany>();

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        Companies = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostSecAsync(int companyId, string? returnUrl, CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExistsActiveAsync(companyId, cancellationToken).ConfigureAwait(false))
        {
            ErrorMessage = "Geçersiz veya pasif şirket.";
            return RedirectToPage();
        }

        HttpContext.Session.SetSelectedAccountingCompanyId(companyId);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Muhasebe/Index");
    }
}
