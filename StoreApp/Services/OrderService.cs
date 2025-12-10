using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StoreApp.Services
{
    public class OrderService
    {
        private readonly OrderRepository _orderRepo;
        private readonly ActivityLogService _logService;
        private readonly UserRepository _userRepo;
        private readonly CustomerRepository _customerRepo;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            OrderRepository orderRepo,
            ActivityLogService logService,
            UserRepository userRepo,
            CustomerRepository customerRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _orderRepo = orderRepo;
            _logService = logService;
            _customerRepo = customerRepo;
            _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
        }


        /*
            Phương thúc Tạo đơn hàng mới 
            Trả về object OrderDTO vừa tạo có 
                Id: Lấy max Id + 1
                OrderNumber: DH_Id_timestamp <Ví dụ: DH_5_1696543200>
                CustomerId: 0 => Mặc định là khách vãng lai
                UserId: Nhân viên đang tạo đơn hàng (Tạm thời để là 2 vì chưa biết ai đang làm đăng nhập . Tạm thời để đó)
                Status: pending
                Subtotal, Discount, TotalAmount: 0m
                PromotionId: null
                Note: null
                CreatedAt, UpdatedAt: thời gian hiện tại

        */

        private int GetCurrentUserId()
        {
            var context = _httpContextAccessor.HttpContext;

            // check claim JWT
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("uid")
                            ?? context.User.FindFirst("userId")
                            ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (claim != null && int.TryParse(claim.Value, out int id))
                    return id;
            }

            // fallback đọc từ header
            var headerUid = context?.Request?.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerUid) && int.TryParse(headerUid, out int headerId))
                return headerId;

            throw new InvalidOperationException("Không tìm thấy user_id trong token");
        }

        /*
            Phương thúc Tạo đơn hàng mới 
            Trả về object OrderDTO vừa tạo có 
                Id: Lấy max Id + 1
                OrderNumber: Guid.NewGuid().ToString()
                CustomerId: 0 => Mặc định là khách vãng lai
                UserId: Nhân viên đang tạo đơn hàng (Tạm thời để là 2 vì chưa biết ai đang làm đăng nhập . Tạm thời để đó)
                Status: pending
                Subtotal, Discount, TotalAmount: 0m
                PromotionId: null
                Note: null
                CreatedAt, UpdatedAt: thời gian hiện tại

        */
        // Hàm tạo đơn tạm cho khách khi mua online
 
        public async Task<OrderDTO> CreateTemporaryOrderAsync()
        {
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;
            string orderCode = Guid.NewGuid().ToString();

            // Lấy user_id thực tế
            int userId = 2;
            try
            {
                userId=GetCurrentUserId();
            }
            catch
            {
                
            }
            var user = await _userRepo.GetByIdAsync(userId);
            string userName = user?.FullName ?? $"Nhân viên #{userId}";

            int customerId = 0;
            var customer = await _customerRepo.GetByIdAsync(customerId);
            string customerName = customer?.FullName ?? "Khách vãng lai";

            var tempOrder = new OrderDTO
            {
                Id = newId,
                OrderNumber = orderCode,
                CustomerId = customerId,
                UserId = userId,
                Status = "pending",
                Subtotal = 0m,
                Discount = 0m,
                TotalAmount = 0m,
                PromotionId = null,
                Note = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CustomerName = customerName,
                UserName = userName,
                PromotionCode = null,
                PaymentMethod="cash",
                PaymentStatus="pending",
                TransactionRef=null
            };

            return tempOrder;
        }


                // Lưu đơn hàng (frontend đã gửi đủ dữ liệu)
        public async Task<bool> CreateOrderAsync(OrderDTO dto)
        {
            var order = new Order
            {
                Id           = dto.Id,
                OrderNumber  = dto.OrderNumber,
                CustomerId   = dto.CustomerId,
                UserId       = dto.UserId,
                Status       = dto.Status,
                Subtotal     = dto.Subtotal,
                Discount     = dto.Discount,
                TotalAmount  = dto.TotalAmount,
                PromotionId  = dto.PromotionId,
                Note         = dto.Note,
                CreatedAt    = dto.CreatedAt,
                UpdatedAt    = dto.UpdatedAt
            };

            return await _orderRepo .SaveOrderAsync(order);
        }



        // Chuyển Order sang OrderDTO đầy đủ
        public async Task<OrderDTO> MapToDTOAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Không tìm thấy đơn hàng");

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId ?? 0,
                UserId = order.UserId,
                Status = order.Status,
                Subtotal = order.Subtotal,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                PromotionId = order.PromotionId,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CustomerName = order.Customer?.FullName,
                UserName = order.User?.FullName,
                PromotionCode = order.Promotion?.Code
            };
        }

        // Method cho AI Tool
        public async Task<PagedResult<OrderDTO>> GetPagedOrdersAsync(
            int pageNumber, int pageSize, string? status = null, 
            DateTime? startDate = null, DateTime? endDate = null, string? search = null)
        {
            var orders = await _orderRepo.GetAllAsync();
            
            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status).ToList();
            
            if (startDate.HasValue)
                orders = orders.Where(o => o.CreatedAt >= startDate.Value).ToList();
            
            if (endDate.HasValue)
                orders = orders.Where(o => o.CreatedAt <= endDate.Value).ToList();
            
            if (!string.IsNullOrEmpty(search))
                orders = orders.Where(o => o.OrderNumber.Contains(search)).ToList();
            
            var total = orders.Count;
            var items = orders.Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(o => new OrderDTO 
                { 
                    Id = o.Id, 
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt,
                    CustomerName = o.Customer?.FullName
                }).ToList();
            
            return new PagedResult<OrderDTO> { TotalItems = total, Items = items };
        }

        /// <summary>
        /// Lấy đơn hàng theo CustomerId - dùng cho Customer AI
        /// </summary>
        public async Task<PagedResult<OrderDTO>> GetOrdersByCustomerIdAsync(
            int customerId, int page = 1, int pageSize = 10, string? status = null)
        {
            var orders = await _orderRepo.GetAllAsync();
            
            // Lọc theo customerId
            orders = orders.Where(o => o.CustomerId == customerId).ToList();
            
            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status).ToList();
            
            var total = orders.Count;
            var items = orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDTO 
                { 
                    Id = o.Id, 
                    OrderNumber = o.OrderNumber,
                    CustomerId = o.CustomerId ?? 0,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    Discount = o.Discount,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt,
                    CustomerName = o.Customer?.FullName
                }).ToList();
            
            return new PagedResult<OrderDTO> { TotalItems = total, Items = items };
        }

        /// <summary>
        /// Lấy đơn hàng theo OrderNumber - dùng cho Customer AI
        /// </summary>
        public async Task<OrderDTO?> GetByOrderNumberAsync(string orderNumber)
        {
            var orders = await _orderRepo.GetAllAsync();
            var order = orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            
            if (order == null) return null;
            
            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId ?? 0,
                Status = order.Status,
                Subtotal = order.Subtotal,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                CustomerName = order.Customer?.FullName
            };
        }
        //Phân trang kết hợp tìm kiếm 
        public async Task<ResultPaginatedDTO<OrderDTO> > GetPagedOrdersAsyncForOrderPage(
            int pageNumber,
            int pageSize,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? search
        )
        {
            var (data, totalItems) = await _orderRepo.SearchPagingAsync(
                pageNumber, pageSize, status, startDate, endDate, search
            );

            return new ResultPaginatedDTO<OrderDTO> 
            {
                Items = data,
                TotalItems  = totalItems,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }




    }
}