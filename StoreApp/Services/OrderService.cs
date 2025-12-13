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
            _paymentRepo=paymentRepository;
            _httpContextAccessor = httpContextAccessor;
        }


        /*
            PhÆ°Æ¡ng thÃºc Táº¡o Ä‘Æ¡n hÃ ng má»›i 
            Tráº£ vá» object OrderDTO vá»«a táº¡o cÃ³ 
                Id: Láº¥y max Id + 1
                OrderNumber: DH_Id_timestamp <VÃ­ dá»¥: DH_5_1696543200>
                CustomerId: 0 => Máº·c Ä‘á»‹nh lÃ  khÃ¡ch vÃ£ng lai
                UserId: NhÃ¢n viÃªn Ä‘ang táº¡o Ä‘Æ¡n hÃ ng (Táº¡m thá»i Ä‘á»ƒ lÃ  2 vÃ¬ chÆ°a biáº¿t ai Ä‘ang lÃ m Ä‘Äƒng nháº­p . Táº¡m thá»i Ä‘á»ƒ Ä‘Ã³)
                Status: pending
                Subtotal, Discount, TotalAmount: 0m
                PromotionId: null
                Note: null
                CreatedAt, UpdatedAt: thá»i gian hiá»‡n táº¡i

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

        /*
            PhÆ°Æ¡ng thÃºc Táº¡o Ä‘Æ¡n hÃ ng má»›i 
            Tráº£ vá» object OrderDTO vá»«a táº¡o cÃ³ 
                Id: Láº¥y max Id + 1
                OrderNumber: Guid.NewGuid().ToString()
                CustomerId: 0 => Máº·c Ä‘á»‹nh lÃ  khÃ¡ch vÃ£ng lai
                UserId: NhÃ¢n viÃªn Ä‘ang táº¡o Ä‘Æ¡n hÃ ng (Táº¡m thá»i Ä‘á»ƒ lÃ  2 vÃ¬ chÆ°a biáº¿t ai Ä‘ang lÃ m Ä‘Äƒng nháº­p . Táº¡m thá»i Ä‘á»ƒ Ä‘Ã³)
                Status: pending
                Subtotal, Discount, TotalAmount: 0m
                PromotionId: null
                Note: null
                CreatedAt, UpdatedAt: thá»i gian hiá»‡n táº¡i

        */
        // HÃ m táº¡o Ä‘Æ¡n táº¡m cho Ä‘Æ¡n hÃ ng POS (nhÃ¢n viÃªn táº¡o)
 
        public async Task<OrderDTO> CreateTemporaryOrderAsync()
        {
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;
            string orderCode = Guid.NewGuid().ToString();

            // Láº¥y staff_id thá»±c táº¿
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
                PaymentMethod="cash",
                PaymentStatus="pending",
                TransactionRef=null
            };

            return tempOrder;
        }

        /// <summary>
        /// Táº¡o Ä‘Æ¡n hÃ ng táº¡m thá»i cho khÃ¡ch mua online (Ä‘Ã£ Ä‘Äƒng nháº­p)
        /// - Customer: láº¥y tá»« customerId trong token
        /// - Staff: null (Ä‘Æ¡n online khÃ´ng cÃ³ nhÃ¢n viÃªn)
        /// </summary>
        public async Task<OrderDTO> CreateTemporaryOnlineOrderAsync()
        {
            // 1. Láº¥y CustomerId tá»« token
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch
            {
                throw new UnauthorizedAccessException("KhÃ´ng thá»ƒ xÃ¡c Ä‘á»‹nh khÃ¡ch hÃ ng. Vui lÃ²ng Ä‘Äƒng nháº­p láº¡i.");
            }

            var customer = (await _customerRepo.GetByIdAsync(customerId))
               ?? throw new InvalidOperationException($"KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin khÃ¡ch hÃ ng cho customerId {customerId}");


            if (!customer.IsActive)
                throw new InvalidOperationException("TÃ i khoáº£n khÃ¡ch hÃ ng Ä‘Ã£ bá»‹ khÃ³a.");

            // 3. NhÃ¢n viÃªn máº·c Ä‘á»‹nh cho Ä‘Æ¡n online (Id = 0 hoáº·c báº¡n cÃ³ thá»ƒ táº¡o má»™t User tÃªn "Há»‡ thá»‘ng" hoáº·c "Online")
            int staffId = 0;
            var staff = await _userRepo.GetByIdAsync(staffId);
            string staffName = staff?.FullName ?? "Há»‡ thá»‘ng Online";

            // 4. Táº¡o mÃ£ Ä‘Æ¡n vÃ  Id má»›i
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;
            string orderNumber = Guid.NewGuid().ToString();

            // 5. Táº¡o OrderDTO vá»›i Ä‘áº§y Ä‘á»§ thÃ´ng tin khÃ¡ch hÃ ng
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

                // ThÃ´ng tin hiá»ƒn thá»‹ khÃ¡ch hÃ ng
                CustomerName = customer.FullName??"N/A",
                SoDienThoai = customer.Phone,
                Email = customer.Email,
                DiaChiKhachHang = customer.Address,

                // ThÃ´ng tin nhÃ¢n viÃªn
                StaffName = staffName,

                PromotionCode = null,
                PaymentMethod = "cash",          
                PaymentStatus = "pending",
                TransactionRef = null
            };

            return onlineOrder;
        }


        // LÆ°u Ä‘Æ¡n hÃ ng (frontend Ä‘Ã£ gá»­i Ä‘á»§ dá»¯ liá»‡u)
        public async Task<OrderDTO?> CreateOrderAsync(OrderDTO dto)
        {
                        // generate id/order number if missing
            if (dto.Id == 0)
            {
                int maxId = await _orderRepo.GetMaxIdAsync();
                dto.Id = maxId + 1;
            }
            if (string.IsNullOrWhiteSpace(dto.OrderNumber))
            {
                dto.OrderNumber = Guid.NewGuid().ToString();
            }
            if (dto.CustomerId == null || dto.CustomerId == 0)
            {
                try { dto.CustomerId = GetCurrentCustomerId(); } catch { }
            }
            if (dto.StaffId == null || dto.StaffId == 0)
            {
                try { dto.StaffId = GetCurrentUserId(); } catch { dto.StaffId = null; }
            }
            dto.CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt;
            dto.UpdatedAt = DateTime.UtcNow;

            var order = new Order
            {
                Id           = dto.Id,
                OrderNumber  = dto.OrderNumber,
                CustomerId   = dto.CustomerId,
                StaffId      = dto.StaffId,
                Status       = dto.Status,
                Subtotal     = dto.Subtotal,
                Discount     = dto.Discount,
                TotalAmount  = dto.TotalAmount,
                PromotionId  = dto.PromotionId,
                Note         = dto.Note,
                CreatedAt    = dto.CreatedAt,
                UpdatedAt    = dto.UpdatedAt
            };

            var saved = await _orderRepo.SaveOrderAsync(order);
            return saved ? dto : null;
        }



        // Chuyá»ƒn Order sang OrderDTO Ä‘áº§y Ä‘á»§
        public async Task<OrderDTO> MapToDTOAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng");

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

        /// <summary>
        /// Láº¥y Ä‘Æ¡n hÃ ng theo CustomerId - dÃ¹ng cho Customer AI
        /// </summary>
        public async Task<PagedResult<OrderDTO>> GetOrdersByCustomerIdAsync(
            int customerId, int page = 1, int pageSize = 10, string? status = null)
        {
            var orders = await _orderRepo.GetAllAsync();
            
            // Lá»c theo customerId
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
        /// Láº¥y Ä‘Æ¡n hÃ ng theo OrderNumber - dÃ¹ng cho Customer AI
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
        //PhÃ¢n trang káº¿t há»£p tÃ¬m kiáº¿m 
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

        
        /* Service xá»­ lÃ½ Ä‘Æ¡n hÃ ng Ä‘Ã£ tÃ­ch há»£p láº¥y UserId vÃ  cáº­p nháº­t UserId vÃ o order */
        public async Task<bool> HandleProcessOrderAsync(OrderDTO order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // 1. Láº¥y ngÆ°á»i dÃ¹ng hiá»‡n táº¡i
            int currentUserId = 2;
            try
            {
                currentUserId=GetCurrentUserId();
            }
            catch
            {
                
            }
          

            // 2. Láº¥y order tháº­t trong DB
            var existingOrder = await _orderRepo.GetByIdAsync(order.Id);
            if (existingOrder == null)
                return false;

            // 3. Ghi láº¡i StaffId vÃ o order
            await _orderRepo.UpdateOrderStaffAsync(order.Id, currentUserId);

            // 4. Xá»­ lÃ½ theo phÆ°Æ¡ng thá»©c thanh toÃ¡n
            string method = order.PaymentMethod?.ToLower();

            if (method == "cash")
            {
                // Cáº­p nháº­t tráº¡ng thÃ¡i order
                await _orderRepo.UpdateOrderStatusAsync(order.Id, "paid");

                // Cáº­p nháº­t tráº¡ng thÃ¡i payment
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


        // Há»§y Ä‘Æ¡n
        public async Task<bool> CancelOrderAsync(int orderId)
        {
            // 1. Láº¥y user hiá»‡n táº¡i
            int currentUserId = 2;
            try
            {
                currentUserId=GetCurrentUserId();
            }
            catch
            {
                
            }

            // 2. Kiá»ƒm tra order tá»“n táº¡i
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                return false;

            // 3. Ghi láº¡i ai lÃ  ngÆ°á»i há»§y Ä‘Æ¡n (staff)
            await _orderRepo.UpdateOrderStaffAsync(orderId, currentUserId);

            // 4. Tiáº¿n hÃ nh há»§y Ä‘Æ¡n
            return await _orderRepo.CancelOrderAsync(orderId);
        }

        // Láº¥y Ä‘Æ¡n hÃ ng theo orderId
        public async Task<OrderDTO?> GetOrderDtoByIdAsync_MA(int orderId)
        {
            var order = await _orderRepo.GetOrderDtoByIdAsync_MA(orderId);

            if (order == null)
                return null;

            return order;
        }








    }
}




