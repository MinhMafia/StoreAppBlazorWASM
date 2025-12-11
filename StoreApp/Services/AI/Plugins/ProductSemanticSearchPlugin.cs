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

        [KernelFunction("search_products")]
        [Description("Tìm sản phẩm theo mô tả, nhu cầu, triệu chứng hoặc tìm sản phẩm tương tự. Ví dụ: 'thuốc nhức đầu', 'thuốc ho cho trẻ', 'có sản phẩm nào giống X không?'")]
        public async Task<object> SearchProductsAsync(
            [Description("Câu truy vấn hoặc mô tả sản phẩm cần tìm")] string query,
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


    }
}
