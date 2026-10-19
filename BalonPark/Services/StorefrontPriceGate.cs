namespace BalonPark.Services;

/// <summary>
/// Müşteri vitrininde fiyatların görünüp görünmeyeceği. Kapalıyken fiyat yerine iletişim metni gösterilir.
/// </summary>
public interface IStorefrontPriceGate
{
    bool Visible { get; }
}

public sealed class StorefrontPriceGate(IPriceSettingsStore store) : IStorefrontPriceGate
{
    public bool Visible
    {
        get
        {
            try
            {
                return store.GetSnapshotAsync().GetAwaiter().GetResult().ShowPrices;
            }
            catch
            {
                return true;
            }
        }
    }
}
