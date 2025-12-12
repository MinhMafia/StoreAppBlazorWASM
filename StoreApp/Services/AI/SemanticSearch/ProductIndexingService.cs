using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Services.AI.Embeddings;
using StoreApp.Services.AI.VectorStore;

namespace StoreApp.Services.AI.SemanticSearch
{
    public class ProductIndexingService : IProductIndexingService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStoreService _vectorStoreService;
        private readonly ProductRepository _productRepository;
        private readonly ILogger<ProductIndexingService> _logger;
        private static string CollectionName => AiConstants.ProductCollectionName;

        public ProductIndexingService(
            IEmbeddingService embeddingService,
            IVectorStoreService vectorStoreService,
            ProductRepository productRepository,
            ILogger<ProductIndexingService> logger)
        {
            _embeddingService = embeddingService;
            _vectorStoreService = vectorStoreService;
            _productRepository = productRepository;
            _logger = logger;
        }

        private static string BuildProductText(Product p)
        {
            var parts = new List<string>
            {
                $"Tên: {p.ProductName}",
                p.Sku != null ? $"Mã: {p.Sku}" : "",
                p.Description != null ? $"Mô tả: {p.Description}" : "",
                p.Category?.Name != null ? $"Danh mục: {p.Category.Name}" : "",
                p.Supplier?.Name != null ? $"Nhà cung cấp: {p.Supplier.Name}" : "",
                p.Unit?.Name != null ? $"Đơn vị: {p.Unit.Name}" : ""
            };

            return string.Join(". ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static Dictionary<string, object> BuildProductPayload(Product p)
        {
            return new Dictionary<string, object>
            {
                ["product_id"] = p.Id,
                ["name"] = p.ProductName,
                ["sku"] = p.Sku ?? "",
                ["category_id"] = p.CategoryId ?? 0,
                ["category_name"] = p.Category?.Name ?? "",
                ["supplier_id"] = p.SupplierId ?? 0,
                ["supplier_name"] = p.Supplier?.Name ?? "",
                ["unit_id"] = p.UnitId ?? 0,
                ["unit_name"] = p.Unit?.Name ?? "",
                ["price"] = (double)p.Price,
                ["cost"] = (double)(p.Cost ?? 0),
                ["is_active"] = p.IsActive
            };
        }

        public async Task IndexProductAsync(Product product, CancellationToken ct = default)
        {
            try
            {
                await _vectorStoreService.EnsureCollectionAsync(CollectionName, ct);

                var text = BuildProductText(product);
                var vector = await _embeddingService.GetEmbeddingAsync(text, ct);

                var payload = BuildProductPayload(product);

                await _vectorStoreService.UpsertAsync(CollectionName, product.Id, vector, payload, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index product {ProductId}", product.Id);
                throw;
            }
        }

        public async Task DeleteProductAsync(int productId, CancellationToken ct = default)
        {
            try
            {
                await _vectorStoreService.DeleteAsync(CollectionName, productId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product vector: {ProductId}", productId);
            }
        }

        public async Task<ReindexResultDTO> ReindexAllProductsAsync(CancellationToken ct = default)
        {
            var result = new ReindexResultDTO { StartTime = DateTime.UtcNow };
            const int batchSize = 100;
            int skip = 0;
            int totalIndexed = 0;
            int totalFailed = 0;

            try
            {
                await _vectorStoreService.EnsureCollectionAsync(CollectionName, ct);
                var totalProducts = await _productRepository.GetTotalCountAsync();

                while (!ct.IsCancellationRequested)
                {
                    var products = await _productRepository.GetAllForIndexingAsync(skip, batchSize);
                    if (!products.Any()) break;

                    var texts = products.Select(BuildProductText).ToList();
                    var vectors = await _embeddingService.GetEmbeddingsAsync(texts, ct);

                    var items = products.Zip(vectors, (p, v) => (
                        id: p.Id,
                        vector: v,
                        payload: BuildProductPayload(p)
                    )).ToList();

                    await _vectorStoreService.UpsertBatchAsync(CollectionName, items, ct);

                    totalIndexed += products.Count;
                    skip += batchSize;
                }

                result.Success = true;
                result.TotalIndexed = totalIndexed;
                result.TotalFailed = totalFailed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reindex] FAILED at {Indexed} products: {Message}", totalIndexed, ex.Message);
                result.Success = false;
                result.Error = ex.Message;
                result.TotalIndexed = totalIndexed;
            }

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }
    }
}
