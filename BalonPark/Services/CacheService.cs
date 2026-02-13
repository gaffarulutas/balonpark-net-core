using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using BalonPark.Models;

namespace BalonPark.Services;

/// <summary>
/// Bellek tabanlı cache servisi. TTL sonunda kayıt silinir;
/// bir sonraki istekte cache miss olur, veri DB'den yeniden yüklenir ve tekrar cache'lenir.
/// InvalidateAllAsync tüm key'leri temizlemek için key takibi yapar.
/// </summary>
public class CacheService(IMemoryCache cache) : ICacheService
{
    /// <summary>Cache TTL: 3 saat. Süre dolunca entry kaldırılır, sonraki istekte veri baştan yüklenir.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(3);

    /// <summary>Set edilen tüm key'ler; InvalidateAllAsync ve prefix ile temizleme için kullanılır. Eviction'da otomatik çıkarılır.</summary>
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    /// <summary>Absolute expiration + eviction callback (trackedKeys'den kaldırma).</summary>
    private MemoryCacheEntryOptions CreateEntryOptionsWithEviction(string key, CacheItemPriority priority = CacheItemPriority.Normal)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Priority = priority
        };
        options.RegisterPostEvictionCallback((k, _, _, s) =>
        {
            if (s is CacheService cs && k != null)
                cs._trackedKeys.TryRemove(k.ToString() ?? string.Empty, out _);
        }, this);
        return options;
    }

    /// <summary>Prefix ile eşleşen tüm key'leri cache ve trackedKeys'den kaldırır. prefix null/empty ise tümü.</summary>
    private void RemoveByPrefix(string? prefix)
    {
        var toRemove = string.IsNullOrEmpty(prefix)
            ? _trackedKeys.Keys.ToList()
            : _trackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in toRemove)
        {
            cache.Remove(k);
            _trackedKeys.TryRemove(k, out _);
        }
    }

    private void TrackAndSet<T>(string key, T value, CacheItemPriority priority = CacheItemPriority.Normal)
    {
        _trackedKeys.TryAdd(key, 0);
        cache.Set(key, value, CreateEntryOptionsWithEviction(key, priority));
    }

    private void RemoveTracked(string key)
    {
        cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
    }

    // Cache Keys
    private const string PRODUCTS_KEY = "products_all";
    private const string CATEGORIES_KEY = "categories_all";
    private const string SUBCATEGORIES_KEY = "subcategories_all";
    private const string BLOGS_KEY = "blogs_all";
    private const string FEATURED_BLOGS_KEY = "blogs_featured";
    private const string LATEST_BLOGS_KEY = "blogs_latest";
    private const string SETTINGS_KEY = "settings_main";
    private const string PRODUCT_BY_ID_KEY = "product_id_{0}";
    private const string PRODUCT_BY_SLUG_KEY = "product_slug_{0}";
    private const string PRODUCTS_BY_SUBCATEGORY_KEY = "products_subcategory_{0}";
    private const string PRODUCTS_BY_CATEGORY_KEY = "products_category_{0}";
    private const string PRODUCTS_SEARCH_KEY = "products_search_{0}";
    private const string CATEGORY_BY_ID_KEY = "category_id_{0}";
    private const string CATEGORY_BY_SLUG_KEY = "category_slug_{0}";
    private const string SUBCATEGORIES_BY_CATEGORY_ID_KEY = "subcategories_category_id_{0}";
    private const string SUBCATEGORIES_BY_CATEGORY_SLUG_KEY = "subcategories_category_slug_{0}";
    private const string SUBCATEGORY_BY_ID_KEY = "subcategory_id_{0}";
    private const string SUBCATEGORY_BY_SLUG_KEY = "subcategory_slug_{0}";
    private const string BLOG_BY_ID_KEY = "blog_id_{0}";
    private const string BLOG_BY_SLUG_KEY = "blog_slug_{0}";
    private const string BLOGS_SEARCH_KEY = "blogs_search_{0}";

    #region Products

    public async Task<IEnumerable<Product>?> GetProductsAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<Product>>(PRODUCTS_KEY));
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var key = string.Format(PRODUCT_BY_ID_KEY, id);
        return await Task.FromResult(cache.Get<Product>(key));
    }

    public async Task<Product?> GetProductBySlugAsync(string slug)
    {
        var key = string.Format(PRODUCT_BY_SLUG_KEY, slug);
        return await Task.FromResult(cache.Get<Product>(key));
    }

    public async Task<IEnumerable<Product>?> GetProductsBySubCategorySlugAsync(string subCategorySlug)
    {
        var key = string.Format(PRODUCTS_BY_SUBCATEGORY_KEY, subCategorySlug);
        return await Task.FromResult(cache.Get<IEnumerable<Product>>(key));
    }

    public async Task<IEnumerable<Product>?> GetProductsByCategorySlugAsync(string categorySlug)
    {
        var key = string.Format(PRODUCTS_BY_CATEGORY_KEY, categorySlug);
        return await Task.FromResult(cache.Get<IEnumerable<Product>>(key));
    }

    public async Task<IEnumerable<Product>?> SearchProductsAsync(string query)
    {
        var key = string.Format(PRODUCTS_SEARCH_KEY, query.ToLower());
        return await Task.FromResult(cache.Get<IEnumerable<Product>>(key));
    }

    public async Task SetProductsAsync(IEnumerable<Product> products)
    {
        TrackAndSet(PRODUCTS_KEY, products);
        await Task.CompletedTask;
    }

    public async Task SetProductAsync(Product product)
    {
        var idKey = string.Format(PRODUCT_BY_ID_KEY, product.Id);
        var slugKey = string.Format(PRODUCT_BY_SLUG_KEY, product.Slug);
        TrackAndSet(idKey, product);
        TrackAndSet(slugKey, product);
        await Task.CompletedTask;
    }

    #endregion

    #region Categories

    public async Task<IEnumerable<Category>?> GetCategoriesAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<Category>>(CATEGORIES_KEY));
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        var key = string.Format(CATEGORY_BY_ID_KEY, id);
        return await Task.FromResult(cache.Get<Category>(key));
    }

    public async Task<Category?> GetCategoryBySlugAsync(string slug)
    {
        var key = string.Format(CATEGORY_BY_SLUG_KEY, slug);
        return await Task.FromResult(cache.Get<Category>(key));
    }

    public async Task SetCategoriesAsync(IEnumerable<Category> categories)
    {
        TrackAndSet(CATEGORIES_KEY, categories);
        await Task.CompletedTask;
    }

    public async Task SetCategoryAsync(Category category)
    {
        var idKey = string.Format(CATEGORY_BY_ID_KEY, category.Id);
        var slugKey = string.Format(CATEGORY_BY_SLUG_KEY, category.Slug);
        TrackAndSet(idKey, category);
        TrackAndSet(slugKey, category);
        await Task.CompletedTask;
    }

    #endregion

    #region SubCategories

    public async Task<IEnumerable<SubCategory>?> GetSubCategoriesAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<SubCategory>>(SUBCATEGORIES_KEY));
    }

    public async Task<IEnumerable<SubCategory>?> GetSubCategoriesByCategoryIdAsync(int categoryId)
    {
        var key = string.Format(SUBCATEGORIES_BY_CATEGORY_ID_KEY, categoryId);
        return await Task.FromResult(cache.Get<IEnumerable<SubCategory>>(key));
    }

    public async Task<IEnumerable<SubCategory>?> GetSubCategoriesByCategorySlugAsync(string categorySlug)
    {
        var key = string.Format(SUBCATEGORIES_BY_CATEGORY_SLUG_KEY, categorySlug);
        return await Task.FromResult(cache.Get<IEnumerable<SubCategory>>(key));
    }

    public async Task<SubCategory?> GetSubCategoryByIdAsync(int id)
    {
        var key = string.Format(SUBCATEGORY_BY_ID_KEY, id);
        return await Task.FromResult(cache.Get<SubCategory>(key));
    }

    public async Task<SubCategory?> GetSubCategoryBySlugAsync(string slug)
    {
        var key = string.Format(SUBCATEGORY_BY_SLUG_KEY, slug);
        return await Task.FromResult(cache.Get<SubCategory>(key));
    }

    public async Task SetSubCategoriesAsync(IEnumerable<SubCategory> subCategories)
    {
        TrackAndSet(SUBCATEGORIES_KEY, subCategories);
        await Task.CompletedTask;
    }

    public async Task SetSubCategoryAsync(SubCategory subCategory)
    {
        var idKey = string.Format(SUBCATEGORY_BY_ID_KEY, subCategory.Id);
        var slugKey = string.Format(SUBCATEGORY_BY_SLUG_KEY, subCategory.Slug);
        TrackAndSet(idKey, subCategory);
        TrackAndSet(slugKey, subCategory);
        await Task.CompletedTask;
    }

    #endregion

    #region Blogs

    public async Task<IEnumerable<Blog>?> GetBlogsAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<Blog>>(BLOGS_KEY));
    }

    public async Task<Blog?> GetBlogByIdAsync(int id)
    {
        var key = string.Format(BLOG_BY_ID_KEY, id);
        return await Task.FromResult(cache.Get<Blog>(key));
    }

    public async Task<Blog?> GetBlogBySlugAsync(string slug)
    {
        var key = string.Format(BLOG_BY_SLUG_KEY, slug);
        return await Task.FromResult(cache.Get<Blog>(key));
    }

    public async Task<IEnumerable<Blog>?> GetFeaturedBlogsAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<Blog>>(FEATURED_BLOGS_KEY));
    }

    public async Task<IEnumerable<Blog>?> GetLatestBlogsAsync()
    {
        return await Task.FromResult(cache.Get<IEnumerable<Blog>>(LATEST_BLOGS_KEY));
    }

    public async Task<IEnumerable<Blog>?> SearchBlogsAsync(string query)
    {
        var key = string.Format(BLOGS_SEARCH_KEY, query.ToLower());
        return await Task.FromResult(cache.Get<IEnumerable<Blog>>(key));
    }

    public async Task SetBlogsAsync(IEnumerable<Blog> blogs)
    {
        TrackAndSet(BLOGS_KEY, blogs);
        await Task.CompletedTask;
    }

    public async Task SetFeaturedBlogsAsync(IEnumerable<Blog> blogs)
    {
        TrackAndSet(FEATURED_BLOGS_KEY, blogs);
        await Task.CompletedTask;
    }

    public async Task SetLatestBlogsAsync(IEnumerable<Blog> blogs)
    {
        TrackAndSet(LATEST_BLOGS_KEY, blogs);
        await Task.CompletedTask;
    }

    public async Task SetSearchBlogsAsync(string query, IEnumerable<Blog> blogs)
    {
        var key = string.Format(BLOGS_SEARCH_KEY, query.ToLower());
        TrackAndSet(key, blogs);
        await Task.CompletedTask;
    }

    public async Task SetBlogAsync(Blog blog)
    {
        var idKey = string.Format(BLOG_BY_ID_KEY, blog.Id);
        var slugKey = string.Format(BLOG_BY_SLUG_KEY, blog.Slug);
        TrackAndSet(idKey, blog);
        TrackAndSet(slugKey, blog);
        await Task.CompletedTask;
    }

    #endregion

    #region Settings

    public async Task<Settings?> GetSettingsAsync()
    {
        return await Task.FromResult(cache.Get<Settings>(SETTINGS_KEY));
    }

    public async Task SetSettingsAsync(Settings settings)
    {
        TrackAndSet(SETTINGS_KEY, settings, CacheItemPriority.High);
        await Task.CompletedTask;
    }

    #endregion

    #region Cache Invalidation

    public async Task InvalidateProductsAsync()
    {
        RemoveByPrefix("product");
        await Task.CompletedTask;
    }

    public async Task InvalidateProductAsync(int id)
    {
        var idKey = string.Format(PRODUCT_BY_ID_KEY, id);
        RemoveTracked(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateProductBySlugAsync(string slug)
    {
        var slugKey = string.Format(PRODUCT_BY_SLUG_KEY, slug);
        RemoveTracked(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateProductsByCategorySlugAsync(string categorySlug)
    {
        var key = string.Format(PRODUCTS_BY_CATEGORY_KEY, categorySlug);
        RemoveTracked(key);
        await Task.CompletedTask;
    }

    public async Task InvalidateProductsBySubCategorySlugAsync(string subCategorySlug)
    {
        var key = string.Format(PRODUCTS_BY_SUBCATEGORY_KEY, subCategorySlug);
        RemoveTracked(key);
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoriesAsync()
    {
        // Scoped CacheService: _trackedKeys sadece bu istekte set edilen key'leri tutar.
        // "categories_all" başka istekte set edildiği için prefix ile bulunmaz; doğrudan kaldır.
        cache.Remove(CATEGORIES_KEY);
        RemoveByPrefix("categor");
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoryAsync(int id)
    {
        var idKey = string.Format(CATEGORY_BY_ID_KEY, id);
        RemoveTracked(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoryBySlugAsync(string slug)
    {
        var slugKey = string.Format(CATEGORY_BY_SLUG_KEY, slug);
        RemoveTracked(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoriesAsync()
    {
        // Scoped CacheService: liste key'i başka istekte set edilmiş olabilir; doğrudan kaldır.
        cache.Remove(SUBCATEGORIES_KEY);
        RemoveByPrefix("subcategor");
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoryAsync(int id)
    {
        var idKey = string.Format(SUBCATEGORY_BY_ID_KEY, id);
        RemoveTracked(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoryBySlugAsync(string slug)
    {
        var slugKey = string.Format(SUBCATEGORY_BY_SLUG_KEY, slug);
        RemoveTracked(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogsAsync()
    {
        RemoveByPrefix("blog");
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogAsync(int id)
    {
        var idKey = string.Format(BLOG_BY_ID_KEY, id);
        RemoveTracked(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogBySlugAsync(string slug)
    {
        var slugKey = string.Format(BLOG_BY_SLUG_KEY, slug);
        RemoveTracked(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSettingsAsync()
    {
        RemoveTracked(SETTINGS_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateAllAsync()
    {
        // Scoped CacheService: _trackedKeys sadece bu istekte set edilen key'leri tutar.
        // Diğer isteklerde set edilen key'leri silmek için bilinen ana key'leri doğrudan kaldır.
        cache.Remove(SETTINGS_KEY);
        cache.Remove(CATEGORIES_KEY);
        cache.Remove(SUBCATEGORIES_KEY);
        cache.Remove(PRODUCTS_KEY);
        cache.Remove(BLOGS_KEY);
        cache.Remove(FEATURED_BLOGS_KEY);
        cache.Remove(LATEST_BLOGS_KEY);
        RemoveByPrefix(null);
        await Task.CompletedTask;
    }

    #endregion
}
