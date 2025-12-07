using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class OrderPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Tìm kiếm đơn hàng. Xem chi tiết bằng orderId.")]
        public async Task<object> QueryOrders(
            [Description("ID đơn hàng cụ thể")] int? orderId = null,
            [Description("Trạng thái đơn: pending, completed, cancelled")] string? status = null,
            [Description("Từ ngày (yyyy-MM-dd)")] string? dateFrom = null,
            [Description("Đến ngày (yyyy-MM-dd)")] string? dateTo = null,
            [Description("Tìm theo tên/SĐT khách")] string? keyword = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 20)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            if (orderId.HasValue)
            {
                var orderDetail = await orderService.MapToDTOAsync(orderId.Value);
                if (orderDetail == null)
                    return new { error = $"Không tìm thấy đơn hàng #{orderId}" };

                return new
                {
                    order = new
                    {
                        orderDetail.Id,
                        orderDetail.OrderNumber,
                        orderDetail.CustomerName,
                        orderDetail.Status,
                        orderDetail.Subtotal,
                        orderDetail.Discount,
                        orderDetail.TotalAmount,
                        orderDetail.CreatedAt
                    }
                };
            }

            DateTime? startDate = null;
            DateTime? endDate = null;
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
                startDate = df;
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
                endDate = dt;

            var result = await orderService.GetPagedOrdersAsync(
                pageNumber: page,
                pageSize: limit,
                status: status,
                startDate: startDate,
                endDate: endDate,
                search: keyword
            );

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                hasMore = page * limit < result.TotalItems,
                orders = result.Items.Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.CustomerName,
                    o.Status,
                    o.Subtotal,
                    o.Discount,
                    o.TotalAmount,
                    o.CreatedAt
                })
            };
        }
    }
}
