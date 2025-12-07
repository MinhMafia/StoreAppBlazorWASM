using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class CustomerPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomerPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Tìm kiếm khách hàng theo tên, SĐT, email")]
        public async Task<object> QueryCustomers(
            [Description("Tìm theo tên, email, SĐT")] string? keyword = null,
            [Description("Lọc theo trạng thái")] bool? isActive = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 20)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var customerService = scope.ServiceProvider.GetRequiredService<CustomerService>();

            string? status = isActive.HasValue ? (isActive.Value ? "active" : "inactive") : null;

            var result = await customerService.GetFilteredAndPaginatedAsync(
                page: page,
                pageSize: limit,
                keyword: keyword,
                status: status
            );

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                customers = result.Items.Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Phone,
                    c.Email,
                    c.Address,
                    Status = c.IsActive ? "active" : "inactive"
                })
            };
        }
    }
}
