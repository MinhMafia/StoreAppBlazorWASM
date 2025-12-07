using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class ReportsPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public ReportsPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Lấy báo cáo chi tiết theo khoảng thời gian")]
        public async Task<object> GetReports(
            [Description("Loại báo cáo: sales_summary, top_products, top_customers, revenue_by_day")] string type = "sales_summary",
            [Description("Từ ngày (yyyy-MM-dd)")] string? dateFrom = null,
            [Description("Đến ngày (yyyy-MM-dd)")] string? dateTo = null,
            [Description("Số kết quả")] int limit = 10)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var reportsService = scope.ServiceProvider.GetRequiredService<ReportsService>();

            DateTime endDate = DateTime.UtcNow;
            DateTime startDate = endDate.AddDays(-30);

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
                startDate = df;
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
                endDate = dt;

            return type switch
            {
                "sales_summary" => await reportsService.GetSalesSummaryAsync(startDate, endDate),
                "top_products" => await reportsService.GetTopProductsAsync(startDate, endDate, limit),
                "top_customers" => await reportsService.GetTopCustomersAsync(startDate, endDate, limit),
                "revenue_by_day" => await reportsService.GetRevenueByDayAsync(startDate, endDate),
                _ => new { error = $"Loại báo cáo '{type}' không hỗ trợ" }
            };
        }
    }
}
