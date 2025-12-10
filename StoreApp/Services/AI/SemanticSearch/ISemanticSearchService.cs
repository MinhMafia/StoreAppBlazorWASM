using StoreApp.Models;

namespace StoreApp.Services.AI.SemanticSearch
{
    public interface ISemanticSearchService
    {
        Task<List<Product>> SearchProductsAsync(string query, int limit = 20, CancellationToken ct = default);
        Task<List<SemanticSearchResultDTO>> SearchWithScoresAsync(string query, int limit = 20, CancellationToken ct = default);
        Task<long> GetIndexedCountAsync(CancellationToken ct = default);
    }
}
