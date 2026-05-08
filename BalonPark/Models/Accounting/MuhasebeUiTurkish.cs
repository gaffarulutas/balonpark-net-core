namespace BalonPark.Models.Accounting;

/// <summary>Ekranda gösterilecek Türkçe sabit metinler ve hareket kaynağı etiketleri.</summary>
public static class MuhasebeUiTurkish
{
    /// <summary>Veritabanındaki ReferenceType kodunu kullanıcıya Türkçe gösterir.</summary>
    public static string HareketReferansi(string? referenceType)
    {
        if (string.IsNullOrWhiteSpace(referenceType))
            return "—";

        return referenceType.Trim() switch
        {
            AccountingConstants.ReferenceTypeInvoice => "Fatura",
            AccountingConstants.ReferenceTypeInvoiceCancel => "Fatura iptali",
            AccountingConstants.ReferenceTypePayment => "Ödeme / tahsilat",
            AccountingConstants.ReferenceTypeManual => "Manuel kayıt",
            _ => referenceType
        };
    }
}
