using BalonPark.Helpers;
using BalonPark.Models;
using Xunit;

namespace BalonPark.Tests;

public class YouTubeVideoTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?si=abc&v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=12")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ")]
    [InlineData("  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ")]
    public void Accepts_public_youtube_urls_and_stores_canonical_watch_url(string input)
    {
        var ok = YouTubeVideo.TryNormalize(input, out var canonical, out var id);

        Assert.True(ok);
        Assert.Equal("dQw4w9WgXcQ", id);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", canonical);
        Assert.Equal("https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg", YouTubeVideo.ThumbnailUrl(id!));
        Assert.Equal("https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg", YouTubeVideo.FallbackThumbnailUrl(id!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_url_clears_the_video(string? input)
    {
        var product = new Product
        {
            YouTubeUrl = input,
            YouTubeShowFirst = true
        };

        var ok = YouTubeVideo.TryApply(product, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Null(product.YouTubeUrl);
        Assert.False(product.YouTubeShowFirst);
    }

    [Theory]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><script>")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://evil@youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("not a url")]
    public void Rejects_non_youtube_or_malformed_urls(string input)
    {
        var product = new Product { YouTubeUrl = input, YouTubeShowFirst = true };

        var ok = YouTubeVideo.TryApply(product, out var error);

        Assert.False(ok);
        Assert.Equal(YouTubeVideo.InvalidUrlMessage, error);
        Assert.Equal(input, product.YouTubeUrl);
        Assert.True(product.YouTubeShowFirst);
    }

    [Fact]
    public void Rejects_urls_longer_than_the_column()
    {
        var input = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&" + new string('a', Product.YouTubeUrlMaxLength);

        Assert.False(YouTubeVideo.TryNormalize(input, out _, out _));
    }
}
