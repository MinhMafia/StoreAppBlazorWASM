using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class StatisticsPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public StatisticsPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Lấy thống kê: tổng quan, doanh thu, bán chạy, tồn kho thấp")]
        public async Task<object> GetStatistics(
            [Description("Loại thống kê: overview, revenue, best_sellers, low_stock, order_stats")] string type = "overview",
            [Description("Số ngày (mặc định 7)")] int days = 7,
            [Description("Số kết quả")] int limit = 10,
            [Description("Ngưỡng tồn kho")] int threshold = 10)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var statisticsService = scope.ServiceProvider.GetRequiredService<StatisticsService>();

            return type switch
            {
                "overview" => await statisticsService.GetOverviewStatsAsync(),
                "revenue" => await statisticsService.GetRevenueByPeriodAsync(days),
                "best_sellers" => await statisticsService.GetBestSellersAsync(limit, days),
                "low_stock" => await statisticsService.GetLowStockProductsAsync(threshold),
                "order_stats" => await statisticsService.GetOrderStatsAsync(days),
                _ => new { error = $"Loại thống kê '{type}' không hỗ trợ. Dùng: overview, revenue, best_sellers, low_stock, order_stats" }
            };
        }

        [KernelFunction, Description("Kiểm tra tình trạng tồn kho tổng quan")]
        public async Task<object> GetInventoryStatus(
            [Description("Ngưỡng cảnh báo (mặc định 10)")] int threshold = 10,
            [Description("Lọc theo danh mục")] int? categoryId = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var statisticsService = scope.ServiceProvider.GetRequiredService<StatisticsService>();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

            var lowStock = await statisticsService.GetLowStockProductsAsync(threshold);
            var allProducts = await productService.GetPaginatedProductsAsync(
                1, 1000, null, categoryId, null, null, null, "", null
            );

            var totalValue = allProducts.Items
                .Where(p => p.Inventory != null)
                .Sum(p => (p.Inventory?.Quantity ?? 0) * p.Price);

            var outOfStock = allProducts.Items.Count(p => p.Inventory == null || p.Inventory.Quantity <= 0);

            return new
            {
                summary = new
                {
                    totalProducts = allProducts.TotalItems,
                    outOfStockCount = outOfStock,
                    lowStockCount = lowStock.Count(),
                    totalInventoryValue = totalValue
                },
                lowStockProducts = lowStock.Take(20)
            };
        }
    }
}
