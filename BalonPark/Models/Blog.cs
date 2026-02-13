using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models;

public class Blog
{
    public const int TitleMaxLength = 200;
    public const int SlugMaxLength = 250;
    public const int ExcerptMaxLength = 500;
    public const int FeaturedImageMaxLength = 500;
    public const int MetaTitleMaxLength = 200;
    public const int MetaDescriptionMaxLength = 300;
    public const int MetaKeywordsMaxLength = 500;
    public const int AuthorNameMaxLength = 100;
    public const int CategoryMaxLength = 100;

    public int Id { get; set; }

    [StringLength(TitleMaxLength, ErrorMessage = "Başlık en fazla {1} karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(SlugMaxLength, ErrorMessage = "Slug en fazla {1} karakter olabilir.")]
    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [StringLength(ExcerptMaxLength, ErrorMessage = "Özet en fazla {1} karakter olabilir.")]
    public string Excerpt { get; set; } = string.Empty;

    [StringLength(FeaturedImageMaxLength)]
    public string? FeaturedImage { get; set; }

    [StringLength(MetaTitleMaxLength)]
    public string? MetaTitle { get; set; }

    [StringLength(MetaDescriptionMaxLength)]
    public string? MetaDescription { get; set; }

    [StringLength(MetaKeywordsMaxLength)]
    public string? MetaKeywords { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    [StringLength(AuthorNameMaxLength)]
    public string? AuthorName { get; set; } = "Balon Park";
    public List<string>? Tags { get; set; }

    [StringLength(CategoryMaxLength)]
    public string? Category { get; set; }
}

public class BlogWithProducts : Blog
{
    public List<ProductWithImage> RelatedProducts { get; set; } = new();
}
