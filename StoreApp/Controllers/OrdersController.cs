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


    }
}
