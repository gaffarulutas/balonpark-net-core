using BalonPark.Data;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe;

public class IndexModel : BaseMuhasebePage
{
    private readonly CounterpartyRepository _counterpartyRepository;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly AccountMovementRepository _movementRepository;

    public IndexModel(
        AccountingCompanyRepository accountingCompanyRepository,
        CounterpartyRepository counterpartyRepository,
        InvoiceRepository invoiceRepository,
        AccountMovementRepository movementRepository)
        : base(accountingCompanyRepository)
    {
        _counterpartyRepository = counterpartyRepository;
        _invoiceRepository = invoiceRepository;
        _movementRepository = movementRepository;
    }

    public int CounterpartyCount { get; set; }
    public int InvoiceCount { get; set; }
    public IReadOnlyList<AccountMovement> RecentMovements { get; private set; } = Array.Empty<AccountMovement>();

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        CounterpartyCount = await _counterpartyRepository.CountByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
        InvoiceCount = await _invoiceRepository.CountActiveByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
        RecentMovements = await _movementRepository.GetByCompanyAsync(companyId, null, 15, cancellationToken).ConfigureAwait(false);
    }
}
