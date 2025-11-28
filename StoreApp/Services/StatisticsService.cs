using StoreApp.Shared;
using StoreApp.Repository;

namespace StoreApp.Services
{
    public class StatisticsService
    {
        private readonly StatisticsRepository _repository;

        public StatisticsService(StatisticsRepository repository)
        {
            _repository = repository;
        }

        public Task<OverviewStatsDTO> GetOverviewStatsAsync()
        {
            return _repository.GetOverviewStatsAsync();
        }

        public Task<List<RevenueDataPoint>> GetRevenueByPeriodAsync(int days = 7)
        {
            return _repository.GetRevenueByPeriodAsync(days);
        }

        public Task<List<ProductSalesDTO>> GetBestSellersAsync(int limit = 10, int days = 7)
        {
            return _repository.GetBestSellersAsync(limit, days);
        }

        public Task<List<ProductInventoryDTO>> GetLowStockProductsAsync(int threshold = 10)
        {
            return _repository.GetLowStockProductsAsync(threshold);
        }

        public Task<OrderStatsDTO> GetOrderStatsAsync(int days = 7)
        {
            return _repository.GetOrderStatsAsync(days);
        }
    }
}
