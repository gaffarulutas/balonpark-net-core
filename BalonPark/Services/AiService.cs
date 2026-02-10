using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BalonPark.Models;

namespace BalonPark.Services;

public class AiService(HttpClient httpClient, ILogger<AiService> logger, IConfiguration configuration) : IAiService
{
    private readonly string _apiKey = configuration["ChatGPT:ApiKey"] ?? throw new InvalidOperationException("ChatGPT API key not configured");

    // Static constructor equivalent - execute initialization
    private readonly HttpClient _httpClient = InitializeHttpClient(httpClient, configuration["ChatGPT:ApiKey"] ?? throw new InvalidOperationException("ChatGPT API key not configured"));

    private static HttpClient InitializeHttpClient(HttpClient client, string apiKey)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        return client;
    }

    public async Task<ProductAiResponse> GenerateProductContentAsync(string productDescription)
    {
        try
        {
            logger.LogInformation("AI Service: API Key length: {ApiKeyLength}", _apiKey?.Length ?? 0);
            
            if (string.IsNullOrWhiteSpace(productDescription))
            {
                throw new Exception("Ürün açıklaması boş olamaz");
            }

            // ChatGPT API ile gerçek içerik oluştur
            var prompt = $"Ürün açıklaması: {productDescription}. Bu ürün için Türkçe olarak JSON formatında şu bilgileri oluştur: {{\"name\": \"ürün adı\", \"description\": \"en az 140 karakter olan detaylı açıklama\", \"technicalDescription\": \"HTML formatında oldukça uzun ve detaylı teknik açıklama (en az 500 karakter, başlıklar için <h3>, listeler için <ul><li>, kalın yazı için <strong>, paragraflar için <p> kullan, 'Ürün Özellikleri' başlığı kullanma)\", \"dimensions\": \"boyutlar sadece sayıx-sayıx-sayı formatında (örn: 14x6x6m)\", \"suggestedPrice\": 100, \"suggestedStock\": 10}}. Sadece JSON formatında yanıt ver, başka açıklama ekleme.";

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1200,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogInformation("Sending request to ChatGPT API with prompt: {Prompt}", prompt);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("ChatGPT API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"ChatGPT API hatası ({response.StatusCode}): {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInformation("ChatGPT API response: {Response}", responseContent);

            var chatResponse = JsonSerializer.Deserialize<ChatGptResponse>(responseContent);
            
            logger.LogInformation("Deserialized response - Choices count: {Count}", chatResponse?.Choices?.Count ?? 0);
            
            if (chatResponse?.Choices == null || !chatResponse.Choices.Any())
            {
                logger.LogError("No choices in response");
                throw new Exception("ChatGPT API returned no choices");
            }

            var firstChoice = chatResponse.Choices.First();
            logger.LogInformation("First choice message: {Message}", firstChoice?.Message?.Content ?? "NULL");
            
            if (firstChoice?.Message?.Content == null)
            {
                logger.LogError("Message content is null");
                throw new Exception("ChatGPT API returned empty message content");
            }

            var aiContent = firstChoice.Message.Content;
            logger.LogInformation("AI Content: {Content}", aiContent);
            
            if (string.IsNullOrWhiteSpace(aiContent))
            {
                throw new Exception("ChatGPT API returned empty content");
            }
            
            // JSON'u parse et
            try
            {
                var productResponse = JsonSerializer.Deserialize<ProductAiResponse>(aiContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (productResponse == null)
                {
                    throw new Exception("Failed to deserialize AI response");
                }

                logger.LogInformation("Successfully parsed AI response: {Response}", JsonSerializer.Serialize(productResponse));
                return productResponse;
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "JSON parsing error. Content: {Content}", aiContent);
                throw new Exception($"JSON parsing hatası: {jsonEx.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating product content with AI");
            throw new Exception($"Yapay zeka ile içerik oluşturulurken hata oluştu: {ex.Message}");
        }
    }

    public async Task<BlogAiResponse> GenerateBlogContentAsync(string blogTopic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blogTopic))
            {
                throw new Exception("Blog özeti boş olamaz");
            }

            // ChatGPT API ile blog içeriği oluştur - özetten tam blog içeriği üret
            var availableCategories = "Çocuk Parkları, Reklam Balonları, Softplay Oyun Alanı, Şişme Çadırlar, Şişme Parklar, Top Havuzları, Trambolinler";
            var prompt = $"Blog özeti: {blogTopic}. Bu özetten yola çıkarak şişme oyun parkları, çocuk eğlence alanları ve aile aktiviteleri konularında Türkçe olarak JSON formatında şu bilgileri oluştur: {{\"title\": \"çekici blog başlığı (60-70 karakter)\", \"excerpt\": \"geliştirilmiş özet (150-200 karakter)\", \"content\": \"HTML formatında ÇOK DETAYLI ve UZUN blog içeriği (en az 3000 karakter, çok sayıda başlık <h2>, <h3>, <h4>, listeler <ul><li>, kalın yazı <strong>, paragraflar <p>, linkler <a>, tablolar <table>, görsel açıklamaları, kullanım önerileri, güvenlik bilgileri, bakım talimatları, fiyat bilgileri, müşteri yorumları, SSS bölümü içeren kapsamlı rehber)\", \"category\": \"AŞAĞIDAKİ KATEGORİLERDEN BİRİNİ SEÇ: {availableCategories}. Özet içeriğine en uygun olanı seç.\", \"metaTitle\": \"SEO başlığı (50-60 karakter)\", \"metaDescription\": \"SEO açıklaması (150-160 karakter)\", \"metaKeywords\": \"SEO anahtar kelimeleri (virgülle ayrılmış, 8-12 kelime)\", \"tags\": \"Etiketler (virgülle ayrılmış, EN AZ 10 ETİKET, örn: çocuk oyunları, şişme park, eğlence, güvenlik, aile aktiviteleri, doğum günü partisi, çocuk gelişimi, oyun alanı, eğlence merkezi, çocuk etkinlikleri, aile eğlencesi, çocuk güvenliği, oyun grupları, eğlence parkı)\"}}. İçerik çok detaylı, eğlenceli, bilgilendirici ve aile dostu olsun. Sadece JSON formatında yanıt ver, başka açıklama ekleme.";

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 4000,
                temperature = 0.8
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("ChatGPT API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"ChatGPT API hatası ({response.StatusCode}): {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<ChatGptResponse>(responseContent);
            
            if (chatResponse?.Choices == null || !chatResponse.Choices.Any())
            {
                throw new Exception("ChatGPT API returned no choices");
            }

            var firstChoice = chatResponse.Choices.First();
            if (firstChoice?.Message?.Content == null)
            {
                throw new Exception("ChatGPT API returned empty message content");
            }

            var aiContent = firstChoice.Message.Content;
            
            if (string.IsNullOrWhiteSpace(aiContent))
            {
                throw new Exception("ChatGPT API returned empty content");
            }
            
            // JSON'u parse et
            try
            {
                var blogResponse = JsonSerializer.Deserialize<BlogAiResponse>(aiContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (blogResponse == null)
                {
                    throw new Exception("Failed to deserialize AI response");
                }

                return blogResponse;
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "JSON parsing error. Content: {Content}", aiContent);
                throw new Exception($"JSON parsing hatası: {jsonEx.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating blog content with AI");
            throw new Exception($"Yapay zeka ile blog içeriği oluşturulurken hata oluştu: {ex.Message}");
        }
    }
}

public class ChatGptResponse
{
    [JsonPropertyName("choices")]
    public List<ChatGptChoice> Choices { get; set; } = new();
}

public class ChatGptChoice
{
    [JsonPropertyName("message")]
    public ChatGptMessage Message { get; set; } = new();
}

public class ChatGptMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
