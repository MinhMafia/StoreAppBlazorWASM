using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class OrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        // Các phương thức liên quan sẽ được triển khai ở đây.

        // 1. Lấy thông tin đơn hàng theo Id
        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems) // nếu bạn có navigation
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        // 2. Cập nhật trạng thái đơn hàng (VD: pending → paid → completed)
        public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        //3. Lấy max Id hiện tại trong bảng Orders => nếu chưa có đơn hàng nào thì trả về 0
        public async Task<int> GetMaxIdAsync()
        {
            return await _context.Orders.AnyAsync() ? await _context.Orders.MaxAsync(o => o.Id) : 0;
        }

        /*
            4. Tạo đơn hàng mới 

        */
        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }
    }
}