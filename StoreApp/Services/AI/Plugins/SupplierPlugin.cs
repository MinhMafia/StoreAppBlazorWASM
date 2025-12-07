using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class SupplierPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public SupplierPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Lấy danh sách nhà cung cấp")]
        public async Task<object> QuerySuppliers(
            [Description("Tìm theo tên NCC")] string? keyword = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 50)
        {
            limit = Math.Min(limit, 100);

            using var scope = _serviceProvider.CreateScope();
            var supplierService = scope.ServiceProvider.GetRequiredService<SupplierService>();

            var result = await supplierService.GetPaginatedAsync(
                page: page,
                pageSize: limit,
                search: keyword
            );

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                suppliers = result.Items
            };
        }
    }
}
