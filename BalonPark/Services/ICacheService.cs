using BalonPark.Models;

namespace BalonPark.Services;

public interface ICacheService
{
    // Products
    Task<IEnumerable<Product>?> GetProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product?> GetProductBySlugAsync(string slug);
    Task<IEnumerable<Product>?> GetProductsBySubCategorySlugAsync(string subCategorySlug);
    Task<IEnumerable<Product>?> GetProductsByCategorySlugAsync(string categorySlug);
    Task<IEnumerable<Product>?> SearchProductsAsync(string query);
    
    // Categories
    Task<IEnumerable<Category>?> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category?> GetCategoryBySlugAsync(string slug);
    
    // SubCategories
    Task<IEnumerable<SubCategory>?> GetSubCategoriesAsync();
    Task<IEnumerable<SubCategory>?> GetSubCategoriesByCategoryIdAsync(int categoryId);
    Task<IEnumerable<SubCategory>?> GetSubCategoriesByCategorySlugAsync(string categorySlug);
    Task<SubCategory?> GetSubCategoryByIdAsync(int id);
    Task<SubCategory?> GetSubCategoryBySlugAsync(string slug);
    
    // Blogs
    Task<IEnumerable<Blog>?> GetBlogsAsync();
    Task<Blog?> GetBlogByIdAsync(int id);
    Task<Blog?> GetBlogBySlugAsync(string slug);
    Task<IEnumerable<Blog>?> GetFeaturedBlogsAsync();
    Task<IEnumerable<Blog>?> GetLatestBlogsAsync();
    Task<IEnumerable<Blog>?> SearchBlogsAsync(string query);
    
    // Settings
    Task<Settings?> GetSettingsAsync();
    Task SetSettingsAsync(Settings settings);
    Task InvalidateSettingsAsync();
    
    // Cache Management
    Task SetProductsAsync(IEnumerable<Product> products);
    Task SetProductAsync(Product product);
    Task SetCategoriesAsync(IEnumerable<Category> categories);
    Task SetCategoryAsync(Category category);
    Task SetSubCategoriesAsync(IEnumerable<SubCategory> subCategories);
    Task SetSubCategoryAsync(SubCategory subCategory);
    Task SetBlogsAsync(IEnumerable<Blog> blogs);
    Task SetFeaturedBlogsAsync(IEnumerable<Blog> blogs);
    Task SetLatestBlogsAsync(IEnumerable<Blog> blogs);
    Task SetSearchBlogsAsync(string query, IEnumerable<Blog> blogs);
    Task SetBlogAsync(Blog blog);
    
    // Cache Invalidation
    Task InvalidateProductsAsync();
    Task InvalidateProductAsync(int id);
    Task InvalidateProductBySlugAsync(string slug);
    Task InvalidateProductsByCategorySlugAsync(string categorySlug);
    Task InvalidateProductsBySubCategorySlugAsync(string subCategorySlug);
    Task InvalidateCategoriesAsync();
    Task InvalidateCategoryAsync(int id);
    Task InvalidateCategoryBySlugAsync(string slug);
    Task InvalidateSubCategoriesAsync();
    Task InvalidateSubCategoryAsync(int id);
    Task InvalidateSubCategoryBySlugAsync(string slug);
    Task InvalidateBlogsAsync();
    Task InvalidateBlogAsync(int id);
    Task InvalidateBlogBySlugAsync(string slug);
    Task InvalidateAllAsync();
}
