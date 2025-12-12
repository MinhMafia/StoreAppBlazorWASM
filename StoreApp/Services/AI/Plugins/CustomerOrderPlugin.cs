using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    /// <summary>
    /// Plugin xử lý đơn hàng cho Customer AI
    /// BẢO MẬT: Chỉ cho phép customer xem đơn hàng của CHÍNH MÌNH
    /// </summary>
    public class CustomerOrderPlugin
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int? _authenticatedCustomerId;

        public CustomerOrderPlugin(IServiceProvider serviceProvider, int? authenticatedCustomerId)
        {
            _serviceProvider = serviceProvider;
            _authenticatedCustomerId = authenticatedCustomerId;
        }

        [KernelFunction, Description("Xem danh sách đơn hàng của tôi (yêu cầu đăng nhập)")]
        public async Task<object> GetMyOrders(
            [Description("Trạng thái: pending, completed, cancelled")] string? status = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 10)
        {
            // BẢO MẬT: Bắt buộc phải đăng nhập
            if (!_authenticatedCustomerId.HasValue || _authenticatedCustomerId.Value <= 0)
            {
                return new { 
                    error = "Bạn cần đăng nhập để xem đơn hàng của mình",
                    requireLogin = true 
                };
            }

            
            limit = Math.Clamp(limit, 1, 20);
            page = Math.Max(page, 1);

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            var result = await orderService.GetOrdersByCustomerIdAsync(
                customerId: _authenticatedCustomerId.Value,  // Hardcode 
                page: page,
                pageSize: limit,
                status: status
            );

            return new
            {
                total = result.TotalItems,
                page,
                hasMore = page * limit < result.TotalItems,
                orders = result.Items.Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt
                })
            };
        }

        [KernelFunction, Description("Xem chi tiết một đơn hàng của tôi (yêu cầu đăng nhập)")]
        public async Task<object> GetOrderDetail(
            [Description("ID đơn hàng")] int orderId)
        {
            // BẢO MẬT: Bắt buộc phải đăng nhập
            if (!_authenticatedCustomerId.HasValue || _authenticatedCustomerId.Value <= 0)
            {
                return new { 
                    error = "Bạn cần đăng nhập để xem chi tiết đơn hàng",
                    requireLogin = true 
                };
            }

            // Validate orderId
            if (orderId <= 0)
            {
                return new { error = "ID đơn hàng không hợp lệ" };
            }

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            var orderDetail = await orderService.MapToDTOAsync(orderId);
            if (orderDetail == null)
            {
                return new { error = "Không tìm thấy đơn hàng" };
            }

            // BẢO MẬT QUAN TRỌNG: Kiểm tra đơn hàng có thuộc về customer này không
            // TUYỆT ĐỐI không cho phép xem đơn hàng của người khác
            if (orderDetail.CustomerId != _authenticatedCustomerId.Value)
            {
                return new { error = "Bạn không có quyền xem đơn hàng này" };
            }

            return new
            {
                order = new
                {
                    orderDetail.Id,
                    orderDetail.OrderNumber,
                    orderDetail.Status,
                    orderDetail.Subtotal,
                    orderDetail.Discount,
                    orderDetail.TotalAmount,
                    orderDetail.CreatedAt,
                    orderDetail.Note
                }
            };
        }

        [KernelFunction, Description("Tra cứu đơn hàng theo mã đơn (yêu cầu đăng nhập)")]
        public async Task<object> GetOrderByNumber(
            [Description("Mã đơn hàng (ví dụ: ORD-20231207-001)")] string orderNumber)
        {
            // BẢO MẬT: Bắt buộc phải đăng nhập
            if (!_authenticatedCustomerId.HasValue || _authenticatedCustomerId.Value <= 0)
            {
                return new { 
                    error = "Bạn cần đăng nhập để tra cứu đơn hàng",
                    requireLogin = true 
                };
            }

            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return new { error = "Vui lòng nhập mã đơn hàng" };
            }

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            // Tìm đơn hàng theo mã
            var order = await orderService.GetByOrderNumberAsync(orderNumber.Trim());
            if (order == null)
            {
                return new { error = $"Không tìm thấy đơn hàng với mã '{orderNumber}'" };
            }

            // BẢO MẬT: Kiểm tra đơn hàng có thuộc về customer này không
            if (order.CustomerId != _authenticatedCustomerId.Value)
            {
                return new { error = "Bạn không có quyền xem đơn hàng này" };
            }

            var orderDetail = await orderService.MapToDTOAsync(order.Id);

            return new
            {
                order = new
                {
                    orderDetail?.Id,
                    orderDetail?.OrderNumber,
                    orderDetail?.Status,
                    orderDetail?.Subtotal,
                    orderDetail?.Discount,
                    orderDetail?.TotalAmount,
                    orderDetail?.CreatedAt,
                    orderDetail?.Note
                }
            };
        }
    }
}
