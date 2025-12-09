using StoreApp.Models;

namespace StoreApp.Services.AI.SemanticSearch
{
    public interface IProductIndexingService
    {
        Task IndexProductAsync(Product product, CancellationToken ct = default);
        Task DeleteProductAsync(int productId, CancellationToken ct = default);
        Task<ReindexResultDTO> ReindexAllProductsAsync(CancellationToken ct = default);
    }
}
