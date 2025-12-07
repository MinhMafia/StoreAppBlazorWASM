using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
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
                
        public async Task<bool> SaveOrderAsync(Order order)
        {
            try
            {
                _context.Orders.Update(order); 
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 5. Lấy tất cả đơn hàng (cho AI Tool)
        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

                // Tìm kiếm kết hợp phân trang đơn hàng
        public async Task<(List<OrderDTO> Data, int TotalItems)> SearchPagingAsync(
            int pageNumber,
            int pageSize,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? search)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Payments)           // 1-1 relationship
                .AsQueryable();

            // === LỌC TRẠNG THÁI ===
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            // === LỌC TỪ NGÀY ===
            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value.Date);

            // === LỌC ĐẾN NGÀY ===
            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1); // đến 23:59:59.9999999
                query = query.Where(o => o.CreatedAt <= end);
            }

            // === TÌM KIẾM THEO TÊN (không phân biệt hoa thường + trim) ===
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                var pattern = $"%{searchTerm}%";

                query = query.Where(o =>
                    (o.Customer != null && EF.Functions.Like(o.Customer.FullName.ToLower(), pattern)) ||
                    (o.User != null && EF.Functions.Like(o.User.FullName.ToLower(), pattern))
                );
            }

            // === ĐẾM TỔNG SỐ BẢN GHI ===
            int totalItems = await query.CountAsync();

            // === LẤY DỮ LIỆU THEO TRANG + MAP DTO ===
            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDTO
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    UserId = o.UserId,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    Discount = o.Discount,
                    TotalAmount = o.TotalAmount,
                    PromotionId = o.PromotionId,
                    Note = o.Note,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,

                    CustomerName = o.Customer != null ? o.Customer.FullName : null,
                    UserName = o.User != null ? o.User.FullName : null,
                    PromotionCode = o.Promotion != null ? o.Promotion.Code : null,

                    PaymentMethod = o.Payments.FirstOrDefault() != null 
                    ? o.Payments.FirstOrDefault()!.Method 
                    : null,

                    PaymentStatus = o.Payments.FirstOrDefault() != null 
                        ? o.Payments.FirstOrDefault()!.Status 
                        : null,

                    TransactionRef  = o.Payments.FirstOrDefault() != null 
                        ? o.Payments.FirstOrDefault()!.TransactionRef 
                        : null
                })
                .ToListAsync();

            return (data, totalItems);
        }
    }
}