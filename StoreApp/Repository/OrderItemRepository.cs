using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;
using StoreApp.Shared;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StoreApp.Repository
{
    public class OrderItemRepository
    {
        private readonly AppDbContext _context;

        public OrderItemRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lưu nhiều OrderItem
        public async Task<bool> AddOrderItemsAsync(List<OrderItem> items)
        {
            _context.OrderItems.AddRange(items);
            await _context.SaveChangesAsync();
            return true;
        }

        // Lấy danh sách OrderItem theo OrderId
        public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderItems
                .Include(i => i.Product)
                .Where(i => i.OrderId == orderId)
                .ToListAsync();
        }

        // Xóa OrderItem theo OrderId (nếu cần)
        public async Task DeleteByOrderIdAsync(int orderId)
        {
            var items = await _context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();
            if (items.Any())
            {
                _context.OrderItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<List<OrderItemReponse>> GetByOrderIdAsyncVer2(int orderId)
        {
            return await _context.OrderItems
                .Where(x => x.OrderId == orderId)
                .Include(x => x.Product)
                .Select(x => new OrderItemReponse
                {
                    id = x.Id,
                    product = x.Product != null ? x.Product.ProductName : "N/A",
                    qty = x.Quantity,
                    price = x.UnitPrice,
                    total = x.TotalPrice,
                    img = x.Product != null ? x.Product.ImageUrl : "N/A",
                })
                .ToListAsync();
        }



    }
}
