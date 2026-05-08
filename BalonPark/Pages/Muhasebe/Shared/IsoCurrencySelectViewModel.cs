namespace BalonPark.Pages.Muhasebe.Shared;

/// <summary><see cref="_IsoCurrencySelect"/> partial için model.</summary>
public class IsoCurrencySelectViewModel
{
    /// <summary>Model bağlama adı (örn. CreateInvoiceInput.Currency).</summary>
    public required string FieldName { get; init; }

    /// <summary>Seçili ISO kodu (3 harf).</summary>
    public string? Selected { get; init; }

    /// <summary>Etiket metni.</summary>
    public string Label { get; init; } = "Para birimi";

    /// <summary>&lt;select&gt; için id (boşsa FieldName'den türetilir).</summary>
    public string? ControlId { get; init; }

    public string EffectiveId => string.IsNullOrEmpty(ControlId)
        ? FieldName.Replace('.', '_').Replace('[', '_').Replace("]", "")
        : ControlId;

    /// <summary>Alt açıklama satırı (ISO bilgisi).</summary>
    public bool ShowHint { get; init; } = true;
}
