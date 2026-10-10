namespace BalonPark.Helpers;

/// <summary>
/// Admin listesinde sürüklenen ürünlerin site sırasını hesaplar.
/// Görünen ürünler kendi aralarındaki yerleri değiştirir; listede olmayan ürünlerin sırası korunur.
/// </summary>
public static class ProductDisplayOrder
{
    public static IReadOnlyList<(int Id, int DisplayOrder)> PlanUpdates(
        IReadOnlyList<(int Id, int DisplayOrder)> currentOrder,
        IReadOnlyList<int> reorderedIds)
    {
        ArgumentNullException.ThrowIfNull(currentOrder);
        ArgumentNullException.ThrowIfNull(reorderedIds);

        if (reorderedIds.Count == 0)
            throw new ArgumentException("Sıralanacak ürün yok.", nameof(reorderedIds));

        if (reorderedIds.Count != reorderedIds.Distinct().Count())
            throw new ArgumentException("Ürün listesinde tekrar eden kayıt var.", nameof(reorderedIds));

        var indexById = new Dictionary<int, int>(currentOrder.Count);
        for (var i = 0; i < currentOrder.Count; i++)
        {
            if (!indexById.TryAdd(currentOrder[i].Id, i))
                throw new ArgumentException("Katalogda tekrar eden ürün var.");
        }

        var positions = new List<int>(reorderedIds.Count);
        foreach (var id in reorderedIds)
        {
            if (!indexById.TryGetValue(id, out var index))
                throw new ArgumentException($"Ürün bulunamadı: {id}");
            positions.Add(index);
        }

        positions.Sort();
        var ids = new int[currentOrder.Count];
        for (var i = 0; i < currentOrder.Count; i++)
            ids[i] = currentOrder[i].Id;

        for (var i = 0; i < positions.Count; i++)
            ids[positions[i]] = reorderedIds[i];

        var ordersAreUnique = true;
        for (var i = 1; i < currentOrder.Count; i++)
        {
            if (currentOrder[i].DisplayOrder <= currentOrder[i - 1].DisplayOrder)
            {
                ordersAreUnique = false;
                break;
            }
        }

        var updates = new List<(int Id, int DisplayOrder)>();
        if (ordersAreUnique)
        {
            for (var i = 0; i < positions.Count; i++)
            {
                var id = reorderedIds[i];
                var newOrder = currentOrder[positions[i]].DisplayOrder;
                if (currentOrder[indexById[id]].DisplayOrder != newOrder)
                    updates.Add((id, newOrder));
            }

            return updates;
        }

        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var newOrder = i + 1;
            if (currentOrder[indexById[id]].DisplayOrder != newOrder)
                updates.Add((id, newOrder));
        }

        return updates;
    }
}
