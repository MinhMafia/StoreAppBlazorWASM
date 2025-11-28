using StoreApp.Models;
using StoreApp.Repository;

namespace StoreApp.Services
{
    public class OrderItemService
    {
        private readonly OrderItemRepository _orderItemRepository;

        public OrderItemService(OrderItemRepository orderItemRepository)
        {
            _orderItemRepository = orderItemRepository;
        }

        // Lưu list OrderItem
        public async Task<bool> SaveOrderItemsAsync(List<OrderItem> items)
        {
            if (items == null || !items.Any())
                return false;

            await _orderItemRepository.AddOrderItemsAsync(items);
            return true;
        }

        // Lấy theo OrderId
        public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
        {
            return await _orderItemRepository.GetByOrderIdAsync(orderId);
        }

        // Xóa theo OrderId
        public async Task DeleteByOrderIdAsync(int orderId)
        {
            await _orderItemRepository.DeleteByOrderIdAsync(orderId);
        }
    }
}
