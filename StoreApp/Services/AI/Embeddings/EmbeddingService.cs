using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StoreApp.Services.AI.Embeddings
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly string? _model;

        public EmbeddingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmbeddingService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;

            var apiKey = configuration["Embedding:ApiKey"] ?? "";
            var endpoint = configuration["Embedding:Endpoint"] ?? "https://chutes-qwen-qwen3-embedding-0-6b.chutes.ai/v1";
            _model = configuration["Embedding:Model"];

            _httpClient.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            try
            {
                var payload = new
                {
                    input = text,
                    model = _model
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("embeddings", content, ct);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var embeddingJson = doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding");

                var vector = embeddingJson.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                
                _logger.LogDebug("Generated embedding with {Dimensions} dimensions for text: {TextPreview}",
                    vector.Length, text.Length > 50 ? text[..50] + "..." : text);
                
                return vector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding for text: {TextPreview}",
                    text.Length > 50 ? text[..50] + "..." : text);
                throw;
            }
        }

        public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var textList = texts.ToList();
            if (!textList.Any()) return Array.Empty<float[]>();

            try
            {
                var payload = new
                {
                    input = textList,
                    model = _model
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("embeddings", content, ct);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var dataArray = doc.RootElement.GetProperty("data");
                var embeddings = new List<float[]>();

                foreach (var item in dataArray.EnumerateArray())
                {
                    var embeddingJson = item.GetProperty("embedding");
                    var vector = embeddingJson.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    embeddings.Add(vector);
                }

                _logger.LogDebug("Generated {Count} embeddings", embeddings.Count);
                return embeddings.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate batch embeddings for {Count} texts", textList.Count);
                throw;
            }
        }
    }
}
