using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Services.AI.Embeddings;
using StoreApp.Services.AI.VectorStore;

namespace StoreApp.Services.AI.SemanticSearch
{
    public class ProductSemanticSearchService : ISemanticSearchService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStoreService _vectorStoreService;
        private readonly ProductRepository _productRepository;
        private readonly ILogger<ProductSemanticSearchService> _logger;

        public ProductSemanticSearchService(
            IEmbeddingService embeddingService,
            IVectorStoreService vectorStoreService,
            ProductRepository productRepository,
            ILogger<ProductSemanticSearchService> logger)
        {
            _embeddingService = embeddingService;
            _vectorStoreService = vectorStoreService;
            _productRepository = productRepository;
            _logger = logger;
        }

       

        public async Task<List<SemanticSearchResultDTO>> SearchWithScoresAsync(string query, int limit = 20, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<SemanticSearchResultDTO>();

            try
            {
                var queryVector = await _embeddingService.GetEmbeddingAsync(query, ct);
                var filters = new Dictionary<string, object> { ["is_active"] = true };
                var results = await _vectorStoreService.SearchAsync(
                    AiConstants.ProductCollectionName,
                    queryVector,
                    limit,
                    filters,
                    ct);

                if (!results.Any()) return new List<SemanticSearchResultDTO>();

                var ids = results.Select(r => r.id).ToList();
                var products = await _productRepository.GetByIdsAsync(ids);
                var productDict = products.ToDictionary(p => p.Id);

                var searchResults = new List<SemanticSearchResultDTO>();
                foreach (var (id, score, _) in results)
                {
                    if (productDict.TryGetValue(id, out var p))
                    {
                        searchResults.Add(new SemanticSearchResultDTO
                        {
                            ProductId = p.Id,
                            ProductName = p.ProductName,
                            CategoryName = p.Category?.Name,
                            Price = p.Price,
                            ImageUrl = p.ImageUrl,
                            Score = score
                        });
                    }
                }

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for query: {Query}", query);
                return new List<SemanticSearchResultDTO>();
            }
        }

        public async Task<long> GetIndexedCountAsync(CancellationToken ct = default)
        {
            return await _vectorStoreService.GetCountAsync(AiConstants.ProductCollectionName, ct);
        }
    }

    public class SemanticSearchResultDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public float Score { get; set; }
    }

    public class ReindexResultDTO
    {
        public bool Success { get; set; }
        public int TotalIndexed { get; set; }
        public int TotalFailed { get; set; }
        public string? Error { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
