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
        private readonly PaymentRepository _paymentRepo;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            OrderRepository orderRepo,
            ActivityLogService logService,
            UserRepository userRepo,
            CustomerRepository customerRepo,
            PaymentRepository paymentRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _orderRepo = orderRepo;
            _logService = logService;
            _customerRepo = customerRepo;
            _userRepo = userRepo;
            _paymentRepo = paymentRepository;
            _httpContextAccessor = httpContextAccessor;
        }

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

            // fallback Ä‘á»c tá»« header
            var headerUid = context?.Request?.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerUid) && int.TryParse(headerUid, out int headerId))
                return headerId;

            throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y user_id trong token");
        }

        // Láº¥y CustomerId tá»« token (cho customer Ä‘Ã£ Ä‘Äƒng nháº­p)
        private int GetCurrentCustomerId()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("customerId");
                if (claim != null && int.TryParse(claim.Value, out int id))
                    return id;
            }

            throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y customerId trong token");
        }

        public async Task<OrderDTO> CreateTemporaryOrderAsync()
        {
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;
            string orderCode = Guid.NewGuid().ToString();

            int staffId = 2;
            try
            {
                staffId = GetCurrentUserId();
            }
            catch
            {

            }
            var staff = await _userRepo.GetByIdAsync(staffId);
            string staffName = staff?.FullName ?? $"NhÃ¢n viÃªn #{staffId}";

            int customerId = 0;
            var customer = await _customerRepo.GetByIdAsync(customerId);
            string customerName = customer?.FullName ?? "KhÃ¡ch vÃ£ng lai";

            var tempOrder = new OrderDTO
            {
                Id = newId,
                OrderNumber = orderCode,
                CustomerId = customerId,
                StaffId = staffId,
                Status = "pending",
                Subtotal = 0m,
                Discount = 0m,
                TotalAmount = 0m,
                PromotionId = null,
                Note = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CustomerName = customerName,
                StaffName = staffName,
                PromotionCode = null,
                PaymentMethod = "cash",
                PaymentStatus = "pending",
                TransactionRef = null
            };

            return tempOrder;
        }

        public async Task<OrderDTO> CreateTemporaryOnlineOrderAsync()
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch
            {
                throw new UnauthorizedAccessException("Không thấy ID khách hàng, vui lòng nhập lại.");
            }

            var customer = (await _customerRepo.GetByIdAsync(customerId))
               ?? throw new InvalidOperationException($"Không tìm thấy thông tin khách hàng cho customerId {customerId}");


            if (!customer.IsActive)
                throw new InvalidOperationException("Tài khoản khách hàng đã bị khóa.");
            int staffId = 0;
            var staff = await _userRepo.GetByIdAsync(staffId);
            string staffName = staff?.FullName ?? "Hệ thống Online";

            // 4. Tạo mã đơn và Id mới
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;
            string orderNumber = Guid.NewGuid().ToString();

            // 5. Tạo OrderDTO với đầy đủ thông tin khách hàng
            var onlineOrder = new OrderDTO
            {
                Id = newId,
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                StaffId = staffId,
                Status = "pending",
                Subtotal = 0m,
                Discount = 0m,
                TotalAmount = 0m,
                PromotionId = null,
                Note = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,

                CustomerName = customer.FullName ?? "N/A",
                SoDienThoai = customer.Phone,
                Email = customer.Email,
                DiaChiKhachHang = customer.Address,

                StaffName = staffName,

                PromotionCode = null,
                PaymentMethod = "cash",
                PaymentStatus = "pending",
                TransactionRef = null
            };

            return onlineOrder;
        }


        public async Task<OrderDTO?> CreateOrderAsync(OrderDTO dto)
        {
            // generate id/order number if missing
            if (string.IsNullOrWhiteSpace(dto.OrderNumber))
            {
                dto.OrderNumber = Guid.NewGuid().ToString();
            }
            if (dto.CustomerId == null || dto.CustomerId == 0)
            {
                try { dto.CustomerId = GetCurrentCustomerId(); } catch { }
            }

            // StaffId: only set when admin/staff creates the order; customer orders keep StaffId null
            var role = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            bool isStaff = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(role, "staff", StringComparison.OrdinalIgnoreCase);
            if (isStaff)
            {
                if (dto.StaffId == null || dto.StaffId == 0)
                {
                    try { dto.StaffId = GetCurrentUserId(); } catch { dto.StaffId = null; }
                }
            }
            else
            {
                dto.StaffId = null;
            }

            dto.CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt;
            dto.UpdatedAt = DateTime.UtcNow;

            var order = new Order
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                CustomerId = dto.CustomerId,
                StaffId = dto.StaffId,
                Status = dto.Status,
                Subtotal = dto.Subtotal,
                Discount = dto.Discount,
                TotalAmount = dto.TotalAmount,
                PromotionId = dto.PromotionId,
                Note = dto.Note,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            var saved = await _orderRepo.SaveOrderAsync(order);
            if (!saved) return null;

            // EF will populate identity Id on the tracked entity
            dto.Id = order.Id;
            return dto;
        }



        public async Task<OrderDTO> MapToDTOAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Không tìm thấy đơn hàng");

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId ?? 0,
                StaffId = order.StaffId,
                Status = order.Status,
                Subtotal = order.Subtotal,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                PromotionId = order.PromotionId,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CustomerName = order.Customer?.FullName,
                StaffName = order.Staff?.FullName,
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

        public async Task<PagedResult<OrderDTO>> GetOrdersByCustomerIdAsync(
            int customerId, int page = 1, int pageSize = 10, string? status = null)
        {
            var orders = await _orderRepo.GetAllAsync();

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
        public async Task<ResultPaginatedDTO<OrderDTO>> GetPagedOrdersAsyncForOrderPage(
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
                TotalItems = totalItems,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }


        public async Task<bool> HandleProcessOrderAsync(OrderDTO order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // 1. Lấy người dùng hiện tại
            int currentUserId = 2;
            try
            {
                currentUserId = GetCurrentUserId();
            }
            catch
            {

            }


            // 2. Lấy order thật trong DB
            var existingOrder = await _orderRepo.GetByIdAsync(order.Id);
            if (existingOrder == null)
                return false;

            // 3. Ghi lại StaffId vào order
            await _orderRepo.UpdateOrderStaffAsync(order.Id, currentUserId);

            // 4. Xử lý theo phương thức thanh toán
            string method = order.PaymentMethod?.ToLower();

            if (method == "cash")
            {
                // Cập nhật trạng thái order
                await _orderRepo.UpdateOrderStatusAsync(order.Id, "paid");

                // Cập nhật trạng thái payment
                await _paymentRepo.UpdatePaymentStatusByOrderIdAsync(order.Id, "completed");

                return true;
            }

            if (method == "other") // MOMO, VNPAY...
            {
                await _orderRepo.UpdateOrderStatusAsync(order.Id, "paid");
                return true;
            }

            return false;
        }


        public async Task<bool> CancelOrderAsync(int orderId)
        {
            int currentUserId = 2;
            try
            {
                currentUserId = GetCurrentUserId();
            }
            catch
            {

            }

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                return false;

            await _orderRepo.UpdateOrderStaffAsync(orderId, currentUserId);

            return await _orderRepo.CancelOrderAsync(orderId);
        }

        public async Task<OrderDTO?> GetOrderDtoByIdAsync_MA(int orderId)
        {
            var order = await _orderRepo.GetOrderDtoByIdAsync_MA(orderId);

            if (order == null)
                return null;

            return order;
        }

        public async Task<List<OrderDTO>> GetOrdersForCustomerAsync(int customerId)
        {
            return await _orderRepo.GetOrdersByCustomerAsync(customerId);
        }
    }
}

