using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;
using System;
using System.Threading.Tasks;

namespace StoreApp.Services
{
    public class OrderService
    {
        private readonly OrderRepository _orderRepo;
        private readonly ActivityLogService _logService;
        private readonly UserRepository _userRepo;
        private readonly CustomerRepository _customerRepo;

        public OrderService(OrderRepository orderRepo, ActivityLogService logService, UserRepository userRepo,
            CustomerRepository customerRepo)
        {
            _orderRepo = orderRepo;
            _logService = logService;
            _customerRepo = customerRepo;
            _userRepo = userRepo;
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
        public async Task<OrderDTO> CreateTemporaryOrderAsync()
        {
            // Lấy max Id để tạo Id mới
            int maxId = await _orderRepo.GetMaxIdAsync();
            int newId = maxId + 1;

            // Tạo mã đơn hàng dạng UUID thay vì DH_<id>_<timestamp>
            string orderCode = Guid.NewGuid().ToString(); // VD: "f601fbc0-b99a-11f0-820b-141333c764dc"

            // Lấy thông tin User (nhân viên) từ DB
            int userId = 2; // tạm thời
            var user = await _userRepo.GetByIdAsync(userId);
            string userName = user?.FullName ?? "Nhân viên #2";

            // Lấy thông tin Customer (khách vãng lai) từ DB
            int customerId = 0;
            var customer = await _customerRepo.GetByIdAsync(customerId);
            string customerName = customer?.FullName ?? "Khách vãng lai";

            // Tạo object OrderDTO tạm thời (chưa lưu DB)
            var tempOrder = new OrderDTO
            {
                Id = newId,
                OrderNumber = orderCode,      // Dùng UUID
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
                PromotionCode = null
            };

            return tempOrder;
        }

        // Lưu đơn hàng (frontend đã gửi đủ dữ liệu)
        public async Task<Order> SaveOrderAsync(Order order)
        {
            return await _orderRepo.CreateOrderAsync(order);
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
    }
}
