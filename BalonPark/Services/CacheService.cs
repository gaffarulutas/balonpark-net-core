using Microsoft.Extensions.Caching.Memory;
using BalonPark.Models;

namespace BalonPark.Services;

public class CacheService(IMemoryCache cache) : ICacheService
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

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
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };
        
        cache.Set(PRODUCTS_KEY, products, cacheEntryOptions);
        await Task.CompletedTask;
    }

    public async Task SetProductAsync(Product product)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };

        var idKey = string.Format(PRODUCT_BY_ID_KEY, product.Id);
        var slugKey = string.Format(PRODUCT_BY_SLUG_KEY, product.Slug);

        cache.Set(idKey, product, cacheEntryOptions);
        cache.Set(slugKey, product, cacheEntryOptions);
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
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };
        
        cache.Set(CATEGORIES_KEY, categories, cacheEntryOptions);
        await Task.CompletedTask;
    }

    public async Task SetCategoryAsync(Category category)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };

        var idKey = string.Format(CATEGORY_BY_ID_KEY, category.Id);
        var slugKey = string.Format(CATEGORY_BY_SLUG_KEY, category.Slug);

        cache.Set(idKey, category, cacheEntryOptions);
        cache.Set(slugKey, category, cacheEntryOptions);
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
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };
        
        cache.Set(SUBCATEGORIES_KEY, subCategories, cacheEntryOptions);
        await Task.CompletedTask;
    }

    public async Task SetSubCategoryAsync(SubCategory subCategory)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };

        var idKey = string.Format(SUBCATEGORY_BY_ID_KEY, subCategory.Id);
        var slugKey = string.Format(SUBCATEGORY_BY_SLUG_KEY, subCategory.Slug);

        cache.Set(idKey, subCategory, cacheEntryOptions);
        cache.Set(slugKey, subCategory, cacheEntryOptions);
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
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };
        
        cache.Set(BLOGS_KEY, blogs, cacheEntryOptions);
        await Task.CompletedTask;
    }

    public async Task SetBlogAsync(Blog blog)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal
        };

        var idKey = string.Format(BLOG_BY_ID_KEY, blog.Id);
        var slugKey = string.Format(BLOG_BY_SLUG_KEY, blog.Slug);

        cache.Set(idKey, blog, cacheEntryOptions);
        cache.Set(slugKey, blog, cacheEntryOptions);
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
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.High // Settings önemli, yüksek öncelik
        };

        cache.Set(SETTINGS_KEY, settings, cacheEntryOptions);
        await Task.CompletedTask;
    }

    #endregion

    #region Cache Invalidation

    public async Task InvalidateProductsAsync()
    {
        cache.Remove(PRODUCTS_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateProductAsync(int id)
    {
        var idKey = string.Format(PRODUCT_BY_ID_KEY, id);
        cache.Remove(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateProductBySlugAsync(string slug)
    {
        var slugKey = string.Format(PRODUCT_BY_SLUG_KEY, slug);
        cache.Remove(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoriesAsync()
    {
        cache.Remove(CATEGORIES_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoryAsync(int id)
    {
        var idKey = string.Format(CATEGORY_BY_ID_KEY, id);
        cache.Remove(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateCategoryBySlugAsync(string slug)
    {
        var slugKey = string.Format(CATEGORY_BY_SLUG_KEY, slug);
        cache.Remove(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoriesAsync()
    {
        cache.Remove(SUBCATEGORIES_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoryAsync(int id)
    {
        var idKey = string.Format(SUBCATEGORY_BY_ID_KEY, id);
        cache.Remove(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSubCategoryBySlugAsync(string slug)
    {
        var slugKey = string.Format(SUBCATEGORY_BY_SLUG_KEY, slug);
        cache.Remove(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogsAsync()
    {
        cache.Remove(BLOGS_KEY);
        cache.Remove(FEATURED_BLOGS_KEY);
        cache.Remove(LATEST_BLOGS_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogAsync(int id)
    {
        var idKey = string.Format(BLOG_BY_ID_KEY, id);
        cache.Remove(idKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateBlogBySlugAsync(string slug)
    {
        var slugKey = string.Format(BLOG_BY_SLUG_KEY, slug);
        cache.Remove(slugKey);
        await Task.CompletedTask;
    }

    public async Task InvalidateSettingsAsync()
    {
        cache.Remove(SETTINGS_KEY);
        await Task.CompletedTask;
    }

    public async Task InvalidateAllAsync()
    {
        cache.Remove(PRODUCTS_KEY);
        cache.Remove(CATEGORIES_KEY);
        cache.Remove(SUBCATEGORIES_KEY);
        cache.Remove(BLOGS_KEY);
        cache.Remove(FEATURED_BLOGS_KEY);
        cache.Remove(LATEST_BLOGS_KEY);
        cache.Remove(SETTINGS_KEY);
        await Task.CompletedTask;
    }

    #endregion
}
