using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;
using System.ServiceModel.Syndication;
using System.Xml;

namespace BalonPark.Controllers;

[Route("blog")]
public class BlogFeedController(BlogRepository blogRepository, IUrlService urlService) : Controller
{

    [HttpGet("feed")]
    public async Task<IActionResult> Feed()
    {
        try
        {
            // Aktif blog yazılarını getir
            var blogs = await blogRepository.GetAllAsync();
            var activeBlogs = blogs.Where(b => b.IsActive && (b.PublishedAt == null || b.PublishedAt <= DateTime.Now))
                                  .OrderByDescending(b => b.CreatedAt)
                                  .Take(20)
                                  .ToList();

            // RSS feed oluştur
            var feed = new SyndicationFeed(
                title: "Balon Park Blog",
                description: "Şişme oyun parkları, çocuk eğlence alanları ve bakım ipuçları hakkında güncel yazılar",
                feedAlternateLink: new Uri(urlService.GetBaseUrl() + "/blog"),
                id: urlService.GetBaseUrl() + "/blog/feed",
                lastUpdatedTime: activeBlogs.FirstOrDefault()?.CreatedAt ?? DateTime.Now
            );

            feed.Authors.Add(new SyndicationPerson("admin@balonpark.com", "Balon Park", "https://balonpark.com"));
            feed.Copyright = new TextSyndicationContent($"© {DateTime.Now.Year} Balon Park. Tüm hakları saklıdır.");
            feed.Language = "tr-TR";

            // Blog yazılarını RSS item'lara dönüştür
            var items = new List<SyndicationItem>();
            foreach (var blog in activeBlogs)
            {
                var item = new SyndicationItem(
                    title: blog.Title,
                    content: new TextSyndicationContent(blog.Excerpt ?? "", TextSyndicationContentKind.Html),
                    itemAlternateLink: new Uri(urlService.GetBaseUrl() + $"/blog/{blog.Slug}"),
                    id: blog.Id.ToString(),
                    lastUpdatedTime: blog.UpdatedAt ?? blog.CreatedAt
                );

                item.Authors.Add(new SyndicationPerson("admin@balonpark.com", blog.AuthorName ?? "Balon Park", "https://balonpark.com"));
                item.PublishDate = blog.PublishedAt ?? blog.CreatedAt;
                item.Summary = new TextSyndicationContent(blog.Excerpt ?? "", TextSyndicationContentKind.Html);

                // Kategori ekle
                if (!string.IsNullOrEmpty(blog.Category))
                {
                    item.Categories.Add(new SyndicationCategory(blog.Category));
                }

                // Resim ekle
                if (!string.IsNullOrEmpty(blog.FeaturedImage))
                {
                    var imageUrl = urlService.GetImageUrl(blog.FeaturedImage);
                    item.Links.Add(new SyndicationLink(new Uri(imageUrl), "enclosure", "Featured Image", "image/jpeg", 0));
                }

                items.Add(item);
            }

            feed.Items = items;

            // RSS XML'i oluştur
            var settings = new XmlWriterSettings
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var rssFormatter = new Rss20FeedFormatter(feed);
                rssFormatter.WriteTo(writer);
            }

            var content = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return Content(content, "application/rss+xml; charset=utf-8");
        }
        catch (Exception)
        {
            // Hata durumunda boş RSS feed döndür
            var errorFeed = new SyndicationFeed(
                title: "Balon Park Blog - Hata",
                description: "RSS feed yüklenirken hata oluştu",
                feedAlternateLink: new Uri(urlService.GetBaseUrl() + "/blog"),
                id: urlService.GetBaseUrl() + "/blog/feed",
                lastUpdatedTime: DateTime.Now
            );

            var settings = new XmlWriterSettings
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var rssFormatter = new Rss20FeedFormatter(errorFeed);
                rssFormatter.WriteTo(writer);
            }

            var content = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return Content(content, "application/rss+xml; charset=utf-8");
        }
    }
}
