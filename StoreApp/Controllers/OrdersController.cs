using StoreApp.Shared;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Models;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;
        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        //1. Khởi tạo một đối tượng đơn hàng tạm thời để truyền xuống frontend
        [HttpPost("create-temp")]
        public async Task<ActionResult<OrderDTO>> CreateTemporaryOrder()
        {
            try
            {
                var tempOrder = await _orderService.CreateTemporaryOrderAsync();
                return Ok(tempOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("createonlineordertemp")]
        public async Task<ActionResult<OrderDTO>> CreateTemporaryOnlineOrderAsync()
        {
            try
            {
                var tempOrder = await _orderService.CreateTemporaryOnlineOrderAsync();
                return Ok(tempOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Order/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO dto)
        {
            if (dto == null) return BadRequest(false);

            bool success = await _orderService.CreateOrderAsync(dto);

            return Ok(success); // true hoặc false
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            int pageNumber = 1,
            int pageSize = 10,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? search = null
        )
        {
            var result = await _orderService.GetPagedOrdersAsyncForOrderPage(
                pageNumber, pageSize, status, startDate, endDate, search
            );

            return Ok(result);
        }

        /// <summary>
        /// Xử lý đơn hàng (cash hoặc MoMo)
        /// </summary>
        /// <param name="order">OrderDTO gửi từ frontend</param>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessOrder([FromBody] OrderDTO order)
        {
            if (order == null || order.Id <= 0)
                return Ok(false); // trả về false nếu dữ liệu không hợp lệ

            bool success = await _orderService.HandleProcessOrderAsync(order);

            return Ok(success); // true nếu xử lý thành công, false nếu thất bại
        }

        /// <summary>
        /// Hủy đơn hàng pending với payment pending + cash
        /// Trả về true nếu hủy thành công, false nếu không hủy được
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(orderId);

                // Trả về true/false
                return Ok(result);
            }
            catch
            {
                // Nếu lỗi hệ thống, vẫn trả false
                return Ok(false);
            }
        }

        // Lấy đơn hàng theo orderID
        [HttpGet("getOrderByOrderId/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _orderService.GetOrderDtoByIdAsync_MA(orderId);

            if (order == null)
                return NotFound(new { message = $"Order with ID {orderId} not found." });

            return Ok(order);
        }


    }
}
