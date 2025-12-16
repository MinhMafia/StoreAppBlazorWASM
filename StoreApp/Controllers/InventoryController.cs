using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,staff,customer")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;

        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Được dùng khi tạo đơn hàng để trừ kho hàng loạt.
        /// </summary>
        [HttpPost("reduce-multiple")]
        public async Task<IActionResult> ReduceMultipleInventory([FromBody] List<ReduceInventoryDto> items)
        {
            try
            {
                await _inventoryService.ReduceInventoryAsync(items);
                return Ok(new { message = "Cập nhật kho thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách tồn kho kèm thông tin sản phẩm, có phân trang + tìm kiếm + lọc.
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Roles = "admin,staff")]
        public async Task<ActionResult<PaginationResult<InventoryListItemDTO>>> GetInventoryPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "id",
            [FromQuery] string? stockStatus = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _inventoryService.GetInventoryPagedAsync(page, pageSize, search, sortBy, stockStatus);
            return Ok(result);
        }

        /// <summary>
        /// Thống kê tổng quan tồn kho (tổng sản phẩm, hết hàng, sắp hết, còn hàng).
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "admin,staff")]
        public async Task<ActionResult<InventoryStatsDTO>> GetInventoryStats()
        {
            var stats = await _inventoryService.GetInventoryStatsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Điều chỉnh số lượng tồn kho cho 1 sản phẩm (set quantity mới) và ghi log.
        /// </summary>
        [HttpPost("adjust")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AdjustInventory([FromBody] AdjustInventoryRequestDTO request)
        {
            if (request == null)
                return BadRequest(new { message = "Payload không hợp lệ" });

            try
            {
                await _inventoryService.AdjustInventoryAsync(request.InventoryId, request.NewQuantity, request.Reason, User);
                return Ok(new { message = "Cập nhật tồn kho thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

