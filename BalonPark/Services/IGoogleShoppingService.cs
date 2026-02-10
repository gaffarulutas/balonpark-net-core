using BalonPark.Models;

namespace BalonPark.Services
{
    public interface IGoogleShoppingService
    {
        Task<bool> AuthenticateAsync();
        Task<string> CreateProductAsync(GoogleShoppingProduct product);
        Task<string> UpdateProductAsync(GoogleShoppingProduct product);
        Task<bool> DeleteProductAsync(string productId);
        Task<List<GoogleShoppingProduct>> GetAllProductsAsync(bool includeInvalid = false);
        Task<GoogleShoppingProduct?> GetProductAsync(string productId);
        Task<bool> BatchInsertProductsAsync(List<GoogleShoppingProduct> products);
        Task<bool> BatchUpdateProductsAsync(List<GoogleShoppingProduct> products);
        Task<bool> BatchDeleteProductsAsync(List<string> productIds);
        Task<List<GoogleShoppingProduct>> ConvertProductsToGoogleShoppingFormatAsync();
        Task CheckProductStatusesAsync();
        Task<List<GoogleShoppingProduct>> GetProductsForApprovalAsync();
        Task<bool> SubmitProductsForApprovalAsync(List<GoogleShoppingProduct> products);
        Task<Dictionary<string, string>> GetProductApprovalStatusAsync();
    }
}
