namespace BalonPark.Services;

/// <summary>
/// Başka bir siteden yapılan fiyat veya görünürlük değişikliğini, bu sürecin ürün önbelleğine yansıtır.
/// Anlık görüntü 15 saniye tutulur; revizyon değişince ürün ve ayar önbelleği temizlenir.
/// </summary>
public sealed class PriceRevisionMiddleware(RequestDelegate next)
{
    private static long _appliedRevisionTicks;

    public async Task InvokeAsync(HttpContext context, IPriceSettingsStore store, ICacheService cache)
    {
        try
        {
            var snapshot = await store.GetSnapshotAsync(context.RequestAborted).ConfigureAwait(false);
            var ticks = snapshot.Revision.Ticks;
            var previous = Interlocked.Read(ref _appliedRevisionTicks);
            if (previous != 0 && previous != ticks)
            {
                await cache.InvalidateSettingsAsync().ConfigureAwait(false);
                await cache.InvalidateProductsAsync().ConfigureAwait(false);
            }

            Interlocked.Exchange(ref _appliedRevisionTicks, ticks);
        }
        catch
        {
            // Fiyat revizyonu okunamazsa sayfa yine de açılır.
        }

        await next(context).ConfigureAwait(false);
    }
}
