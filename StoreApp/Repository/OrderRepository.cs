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
                await _context.Orders.AddAsync(order); // INSERT đúng
                var result = await _context.SaveChangesAsync();

                return result > 0;  // Chỉ true khi DB thực sự lưu
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lưu Order: " + ex.Message);
                return false;
            }
        }

        // 5. Lấy tất cả đơn hàng (cho AI Tool)
        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Staff)
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
                .Include(o => o.Staff)
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
                    (o.Staff != null && EF.Functions.Like(o.Staff.FullName.ToLower(), pattern))
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
                    StaffId = o.StaffId,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    Discount = o.Discount,
                    TotalAmount = o.TotalAmount,
                    PromotionId = o.PromotionId,
                    Note = o.Note,
                    ShippingAddress = o.ShippingAddress,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                   

                    CustomerName = o.Customer != null ? o.Customer.FullName : null,
                    StaffName = o.Staff != null ? o.Staff.FullName : null,
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




        /// <summary>
        /// Cập nhật staff xử lý đơn hàng theo staff_id bạn truyền vào.
        /// </summary>
        public async Task<bool> UpdateOrderStaffAsync(int orderId, int? newStaffId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return false;

            order.StaffId = newStaffId;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            // Truy vấn gọn, chỉ lấy dữ liệu cần
            var order = await _context.Orders
                .Where(o => o.Id == orderId)
                .Select(o => new 
                {
                    Order = o,
                    Payment = o.Payments.FirstOrDefault(),
                    Items = o.OrderItems
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (order == null)
                return false;

            // Chỉ hủy đơn pending
            if (!string.Equals(order.Order.Status, "pending", StringComparison.OrdinalIgnoreCase))
                return false;

            var payment = order.Payment;

            // Chỉ hủy nếu payment pending hoặc failed
            if (payment == null ||
                !(payment.Status == "pending" || payment.Status == "failed"))
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // Cập nhật trạng thái order 
                order.Order.Status = "cancelled";
                order.Order.UpdatedAt = now;


                // Cộng lại tồn kho
                foreach (var item in order.Items)
                {
                    var inventory = await _context.Inventory
                        .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                    if (inventory != null)
                    {
                        inventory.Quantity += item.Quantity;
                        inventory.UpdatedAt = now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        /// <summary>
        /// Lấy đơn hàng theo orderId
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<OrderDTO?> GetOrderDtoByIdAsync_MA(int orderId)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Staff)
                .Include(o => o.Promotion)
                .Include(o => o.Payments) // 1-n nhưng bạn đang dùng FirstOrDefault
                .AsQueryable();

            var order = await query
                .Where(o => o.Id == orderId)
                .Select(o => new OrderDTO
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    StaffId = o.StaffId,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    Discount = o.Discount,
                    TotalAmount = o.TotalAmount,
                    PromotionId = o.PromotionId,
                    Note = o.Note,
                    ShippingAddress = o.ShippingAddress,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,

                    CustomerName = o.Customer != null ? o.Customer.FullName : null,
                    StaffName = o.Staff != null ? o.Staff.FullName : null,
                    PromotionCode = o.Promotion != null ? o.Promotion.Code : null,

                    PaymentMethod = o.Payments.FirstOrDefault() != null
                        ? o.Payments.FirstOrDefault()!.Method
                        : null,

                    PaymentStatus = o.Payments.FirstOrDefault() != null
                        ? o.Payments.FirstOrDefault()!.Status
                        : null,

                    TransactionRef = o.Payments.FirstOrDefault() != null
                        ? o.Payments.FirstOrDefault()!.TransactionRef
                        : null,
                    
                    DiaChiKhachHang = o.ShippingAddress ?? (o.Customer != null ? o.Customer.Address : null),
                    SoDienThoai = o.Customer != null ? o.Customer.Phone : null,
                    Email=o.Customer != null ? o.Customer.Email : null
                    
                })
                .FirstOrDefaultAsync();

            return order;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng theo customer
        /// </summary>
        public async Task<List<OrderDTO>> GetOrdersByCustomerAsync(int customerId)
        {
            return await _context.Orders
                .Include(o => o.Promotion)
                .Include(o => o.Payments)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDTO
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId,
                    StaffId = o.StaffId,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    Discount = o.Discount,
                    TotalAmount = o.TotalAmount,
                    PromotionId = o.PromotionId,
                    Note = o.Note,
                    ShippingAddress = o.ShippingAddress,
                    PromotionCode = o.Promotion != null ? o.Promotion.Code : null,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    PaymentMethod = o.Payments.OrderByDescending(p => p.Id).Select(p => p.Method).FirstOrDefault(),
                    PaymentStatus = o.Payments.OrderByDescending(p => p.Id).Select(p => p.Status).FirstOrDefault(),
                    CustomerName = o.Customer != null ? o.Customer.FullName : null,
                    SoDienThoai = o.Customer != null ? o.Customer.Phone : null,
                    DiaChiKhachHang = o.ShippingAddress ?? (o.Customer != null ? o.Customer.Address : null)
                })
                .ToListAsync();
        }




    }
}
