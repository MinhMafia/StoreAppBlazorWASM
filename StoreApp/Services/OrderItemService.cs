using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;

namespace StoreApp.Services
{
    public class OrderItemService
    {
        private readonly OrderItemRepository _orderItemRepository;

        public OrderItemService(OrderItemRepository orderItemRepository)
        {
            _orderItemRepository = orderItemRepository;
        }

        // Code cũ
        // // Lưu list OrderItem
        // public async Task<bool> SaveOrderItemsAsync(List<OrderItem> items)
        // {
        //     if (items == null || !items.Any())
        //         return false;

        //     await _orderItemRepository.AddOrderItemsAsync(items);
        //     return true;
        // }

                // Lưu list OrderItem
        public async Task<bool> SaveOrderItemsAsync(List<OrderItemReponse> dtos)
        {
            if (dtos == null || !dtos.Any())
                return false;

            var items = dtos.Select(x => new OrderItem
            {
                OrderId=x.orderid,
                ProductId = x.id,
                Quantity = x.qty,
                UnitPrice = x.price,
                TotalPrice = x.total,
                CreatedAt = DateTime.Now
            }).ToList();

            return await _orderItemRepository.AddOrderItemsAsync(items);
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

        public async Task<List<OrderItemReponse>> GetItemsByOrderAsync(int orderId)
        {
            return await _orderItemRepository.GetByOrderIdAsyncVer2(orderId);
        }
    }
}
