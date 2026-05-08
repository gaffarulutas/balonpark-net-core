using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using BalonPark.Pages.Admin;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BalonPark.Pages.Muhasebe;

/// <summary>Muhasebe alanı: admin oturumu + seçili muhasebe şirketi (session).</summary>
public abstract class BaseMuhasebePage : BaseAdminPage
{
    private readonly AccountingCompanyRepository _accountingCompanyRepository;

    protected BaseMuhasebePage(AccountingCompanyRepository accountingCompanyRepository)
    {
        _accountingCompanyRepository = accountingCompanyRepository;
    }

    /// <summary>Şirket seçimi zorunlu sayfalar için true; şirket listesi / seçim ekranı için false.</summary>
    protected virtual bool RequiresSelectedCompany => true;

    public AccountingCompany? CurrentAccountingCompany { get; private set; }

    public int? SelectedAccountingCompanyId { get; private set; }

    /// <summary>Layout üst çubuğundaki şirket dropdown'ı için tüm şirketler.</summary>
    private async Task PopulateAccountingCompaniesForLayoutAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _accountingCompanyRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        ViewData["AccountingCompanies"] = companies;
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (!RequiresSelectedCompany)
        {
            await PopulateAccountingCompaniesForLayoutAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
            await base.OnPageHandlerExecutionAsync(context, next).ConfigureAwait(false);
            return;
        }

        var id = HttpContext.Session.GetSelectedAccountingCompanyId();
        if (!id.HasValue)
        {
            context.Result = RedirectToPage("/Muhasebe/SirketSec");
            await base.OnPageHandlerExecutionAsync(context, next).ConfigureAwait(false);
            return;
        }

        var company = await _accountingCompanyRepository.GetByIdAsync(id.Value, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (company == null || !company.IsActive)
        {
            HttpContext.Session.RemoveSelectedAccountingCompanyId();
            context.Result = RedirectToPage("/Muhasebe/SirketSec");
            await base.OnPageHandlerExecutionAsync(context, next).ConfigureAwait(false);
            return;
        }

        SelectedAccountingCompanyId = id;
        CurrentAccountingCompany = company;
        ViewData["AccountingCompanyName"] = company.LegalName;
        ViewData["SelectedAccountingCompanyId"] = id.Value;

        await PopulateAccountingCompaniesForLayoutAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
        await base.OnPageHandlerExecutionAsync(context, next).ConfigureAwait(false);
    }
}
