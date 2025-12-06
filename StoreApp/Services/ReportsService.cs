using StoreApp.Shared;
using StoreApp.Shared.DTO;
using StoreApp.Repository;

namespace StoreApp.Services
{
    public class ReportsService
    {
        private readonly ReportsRepository _repository;

        public ReportsService(ReportsRepository repository)
        {
            _repository = repository;
        }

        public Task<List<SalesReportDTO>> GetSalesReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            return _repository.GetSalesReportAsync(fromDate, toDate);
        }

        public Task<List<InventoryReportDTO>> GetInventoryReportAsync()
        {
            return _repository.GetInventoryReportAsync();
        }

        public Task<SalesSummaryDTO> GetSalesSummaryAsync(DateTime? fromDate, DateTime? toDate)
        {
            return _repository.GetSalesSummaryAsync(fromDate, toDate);
        }

        public Task<List<RevenueByDayDTO>> GetRevenueByDayAsync(DateTime? fromDate, DateTime? toDate)
        {
            return _repository.GetRevenueByDayAsync(fromDate, toDate);
        }

        public Task<List<HighValueInventoryDTO>> GetHighValueInventoryAsync(int limit = 10)
        {
            return _repository.GetHighValueInventoryAsync(limit);
        }

        public Task<PeriodComparisonDTO> GetPeriodComparisonAsync(DateTime? fromDate, DateTime? toDate)
        {
            return _repository.GetPeriodComparisonAsync(fromDate, toDate);
        }

        public Task<List<TopProductReportDTO>> GetTopProductsAsync(DateTime? fromDate, DateTime? toDate, int limit = 10)
        {
            return _repository.GetTopProductsAsync(fromDate, toDate, limit);
        }

        public Task<List<TopCustomerReportDTO>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int limit = 10)
        {
            return _repository.GetTopCustomersAsync(fromDate, toDate, limit);
        }

        public Task<List<SalesByStaffDTO>> GetSalesByStaffAsync(DateTime? fromDate, DateTime? toDate)
        {
            return _repository.GetSalesByStaffAsync(fromDate, toDate);
        }
    }
}

