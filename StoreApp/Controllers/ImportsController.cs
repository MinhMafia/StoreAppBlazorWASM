using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportsController : ControllerBase
    {
        private readonly ImportReceiptService _importService;

        public ImportsController(ImportReceiptService importService)
        {
            _importService = importService;
        }

        /// <summary>
        /// Lấy danh sách phiếu nhập có phân trang
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<ActionResult<PaginationResult<ImportListItemDTO>>> GetImports(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null)
        {
            try
            {
                var result = await _importService.GetImportsPagedAsync(page, pageSize, search, status, sortBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi lấy danh sách phiếu nhập: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy chi tiết phiếu nhập
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<ActionResult<ImportDetailDTO>> GetImportDetail(int id)
        {
            try
            {
                var import = await _importService.GetImportDetailAsync(id);
                if (import == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                return Ok(import);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi lấy chi tiết phiếu nhập: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo phiếu nhập mới (dùng khi nhập hàng thủ công từ UI)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<ActionResult> CreateImport([FromBody] CreateImportDTO dto)
        {
            try
            {
                var import = await _importService.CreateImportAsync(dto);
                return Ok(new { message = "Tạo phiếu nhập thành công", importId = import.Id, importNumber = import.ImportNumber });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi tạo phiếu nhập: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái phiếu nhập
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var success = await _importService.UpdateStatusAsync(id, request.Status);
                if (!success)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                return Ok(new { message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi cập nhật: {ex.Message}" });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = "pending";
    }
}
