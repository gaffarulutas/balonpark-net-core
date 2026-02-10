namespace BalonPark.Models;

public class ProductWithImage
{
    public Product Product { get; set; } = null!;
    public ProductImage? MainImage { get; set; }
}
