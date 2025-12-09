using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services.AI.SemanticSearch;

namespace StoreApp.Services.AI.Plugins
{
    public class ProductSemanticSearchPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public ProductSemanticSearchPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction("semantic_search_products")]
        [Description("Tìm sản phẩm theo ý nghĩa, đồng nghĩa. Dùng khi user mô tả triệu chứng, nhu cầu, hoặc tìm kiếm không chính xác tên. Ví dụ: 'thuốc nhức đầu', 'thuốc ho cho trẻ', 'đồ chăm sóc da mụn'")]
        public async Task<object> SemanticSearchProductsAsync(
            [Description("Câu truy vấn bằng tiếng Việt mô tả sản phẩm cần tìm")] string query,
            [Description("Số lượng kết quả tối đa (mặc định 10)")] int limit = 10)
        {
            using var scope = _serviceProvider.CreateScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();
            
            var results = await searchService.SearchWithScoresAsync(query, limit);

            return new
            {
                total = results.Count,
                query,
                products = results.Select(r => new
                {
                    r.ProductId,
                    r.ProductName,
                    r.CategoryName,
                    r.Price,
                    r.ImageUrl,
                    SimilarityScore = r.Score
                })
            };
        }

        [KernelFunction("find_similar_products")]
        [Description("Tìm sản phẩm tương tự với sản phẩm đã cho. Dùng khi user hỏi 'có sản phẩm nào giống X không?'")]
        public async Task<object> FindSimilarProductsAsync(
            [Description("Tên hoặc mô tả sản phẩm gốc để tìm sản phẩm tương tự")] string productDescription,
            [Description("Số lượng kết quả tối đa")] int limit = 5)
        {
            using var scope = _serviceProvider.CreateScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();
            
            var results = await searchService.SearchWithScoresAsync(productDescription, limit + 1);
            var filtered = results.Take(limit).ToList();

            return new
            {
                total = filtered.Count,
                referenceProduct = productDescription,
                similarProducts = filtered.Select(r => new
                {
                    r.ProductId,
                    r.ProductName,
                    r.CategoryName,
                    r.Price,
                    r.ImageUrl,
                    SimilarityScore = r.Score
                })
            };
        }

        [KernelFunction("get_semantic_index_stats")]
        [Description("Lấy thống kê về số lượng sản phẩm đã được index cho tìm kiếm ngữ nghĩa")]
        public async Task<object> GetIndexStatsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();
            
            var count = await searchService.GetIndexedCountAsync();

            return new
            {
                indexedProducts = count,
                status = count > 0 ? "ready" : "empty"
            };
        }
    }
}
