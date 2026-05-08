namespace BalonPark.Models.Accounting;

public static class AccountingValidation
{
    public const decimal InvoiceAmountTolerance = 0.02m;

    public static bool AreInvoiceAmountsConsistent(decimal amountNet, decimal amountVat, decimal amountGross)
    {
        if (amountNet < 0 || amountVat < 0 || amountGross <= 0)
            return false;
        return Math.Abs(amountNet + amountVat - amountGross) <= InvoiceAmountTolerance;
    }
}
