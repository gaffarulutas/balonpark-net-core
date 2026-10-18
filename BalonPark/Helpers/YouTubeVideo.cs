using System.Text.RegularExpressions;
using BalonPark.Models;

namespace BalonPark.Helpers;

/// <summary>
/// Accepts a public YouTube URL and stores only a canonical watch URL built from the video id.
/// </summary>
public static class YouTubeVideo
{
    public const int UrlMaxLength = Product.YouTubeUrlMaxLength;

    public const string InvalidUrlMessage =
        "Geçerli bir YouTube bağlantısı girin (youtube.com veya youtu.be).";

    private static readonly Regex VideoPattern = new(
        @"^(?:https?://)?(?:www\.|m\.)?(?:(?:youtube\.com|youtube-nocookie\.com)/(?:watch\?(?:[^#\s]*&)?v=|embed/|shorts/|live/|v/)|youtu\.be/)([A-Za-z0-9_-]{11})(?:[?&#/][^\s]*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryApply(Product product, out string? error)
    {
        if (!TryNormalize(product.YouTubeUrl, out var canonical, out _))
        {
            error = InvalidUrlMessage;
            return false;
        }

        product.YouTubeUrl = canonical;
        if (canonical == null)
            product.YouTubeShowFirst = false;

        error = null;
        return true;
    }

    public static string? TryGetId(string? value)
    {
        return TryNormalize(value, out _, out var videoId) ? videoId : null;
    }

    public static bool TryNormalize(string? input, out string? canonicalUrl, out string? videoId)
    {
        canonicalUrl = null;
        videoId = null;

        if (string.IsNullOrWhiteSpace(input))
            return true;

        var trimmed = input.Trim();
        if (trimmed.Length > UrlMaxLength)
            return false;

        var match = VideoPattern.Match(trimmed);
        if (!match.Success)
            return false;

        videoId = match.Groups[1].Value;
        canonicalUrl = "https://www.youtube.com/watch?v=" + videoId;
        return true;
    }

    public static string ThumbnailUrl(string videoId) => ThumbnailUrl(videoId, "maxresdefault.jpg");

    public static string FallbackThumbnailUrl(string videoId) => ThumbnailUrl(videoId, "hqdefault.jpg");

    private static string ThumbnailUrl(string videoId, string fileName)
    {
        if (string.IsNullOrEmpty(videoId) || !VideoPattern.IsMatch("https://youtu.be/" + videoId))
            throw new ArgumentException("Geçersiz YouTube video kimliği.", nameof(videoId));

        return "https://i.ytimg.com/vi/" + videoId + "/" + fileName;
    }
}

public sealed record ProductVideoSlide(string VideoId, string Alt, string Label, bool IsThumb)
{
    public string ThumbnailUrl => YouTubeVideo.ThumbnailUrl(VideoId);

    public string FallbackThumbnailUrl => YouTubeVideo.FallbackThumbnailUrl(VideoId);
}
