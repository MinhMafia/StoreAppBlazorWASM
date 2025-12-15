using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared.DTO;
using System.Text;
using OfficeOpenXml;

namespace StoreApp.Controllers
{
    public static class CsvHelper
    {
        public static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            // Nếu có dấu phẩy, dấu ngoặc kép hoặc xuống dòng, cần đặt trong dấu ngoặc kép
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                // Escape dấu ngoặc kép bằng cách nhân đôi
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportsService _reportsService;

        public ReportsController(ReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        // GET api/reports/summary?fromDate=2024-01-01&toDate=2024-01-31
        [HttpGet("summary")]
        public async Task<ActionResult<SalesSummaryDTO>> GetSalesSummary(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var summary = await _reportsService.GetSalesSummaryAsync(fromDate, toDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/revenue-by-day?fromDate=2024-01-01&toDate=2024-01-31
        [HttpGet("revenue-by-day")]
        public async Task<ActionResult<List<RevenueByDayDTO>>> GetRevenueByDay(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _reportsService.GetRevenueByDayAsync(fromDate, toDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/high-value-inventory?limit=100
        [HttpGet("high-value-inventory")]
        public async Task<ActionResult<List<HighValueInventoryDTO>>> GetHighValueInventory(
            [FromQuery] int limit = 100)
        {
            try
            {
                var data = await _reportsService.GetHighValueInventoryAsync(limit);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/period-comparison?fromDate=2024-01-01&toDate=2024-01-31
        [HttpGet("period-comparison")]
        public async Task<ActionResult<PeriodComparisonDTO>> GetPeriodComparison(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _reportsService.GetPeriodComparisonAsync(fromDate, toDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/top-products?fromDate=2024-01-01&toDate=2024-01-31&limit=10
        [HttpGet("top-products")]
        public async Task<ActionResult<List<TopProductReportDTO>>> GetTopProducts(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int limit = 10)
        {
            try
            {
                var data = await _reportsService.GetTopProductsAsync(fromDate, toDate, limit);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/top-customers?fromDate=2024-01-01&toDate=2024-01-31&limit=10
        [HttpGet("top-customers")]
        public async Task<ActionResult<List<TopCustomerReportDTO>>> GetTopCustomers(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int limit = 10)
        {
            try
            {
                var data = await _reportsService.GetTopCustomersAsync(fromDate, toDate, limit);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/sales-by-staff?fromDate=2024-01-01&toDate=2024-01-31
        [HttpGet("sales-by-staff")]
        public async Task<ActionResult<List<SalesByStaffDTO>>> GetSalesByStaff(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _reportsService.GetSalesByStaffAsync(fromDate, toDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/export-sales?fromDate=2024-01-01&toDate=2024-01-31&format=csv
        [HttpGet("export-sales")]
        public async Task<IActionResult> ExportSalesReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string format = "csv")
        {
            try
            {
                var salesData = await _reportsService.GetSalesReportAsync(fromDate, toDate);

                if (format.ToLower() == "xlsx")
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Báo cáo bán hàng");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "Mã đơn";
                    worksheet.Cells[1, 3].Value = "Khách hàng";
                    worksheet.Cells[1, 4].Value = "Sản phẩm";
                    worksheet.Cells[1, 5].Value = "SKU";
                    worksheet.Cells[1, 6].Value = "Số lượng";
                    worksheet.Cells[1, 7].Value = "Đơn giá";
                    worksheet.Cells[1, 8].Value = "Tổng tiền";
                    worksheet.Cells[1, 9].Value = "Chiết khấu";
                    worksheet.Cells[1, 10].Value = "Tổng đơn";
                    worksheet.Cells[1, 11].Value = "Trạng thái";

                    // Data
                    for (int i = 0; i < salesData.Count; i++)
                    {
                        var row = i + 2;
                        worksheet.Cells[row, 1].Value = salesData[i].Date.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 2].Value = salesData[i].OrderNumber;
                        worksheet.Cells[row, 3].Value = salesData[i].CustomerName;
                        worksheet.Cells[row, 4].Value = salesData[i].ProductName;
                        worksheet.Cells[row, 5].Value = salesData[i].Sku;
                        worksheet.Cells[row, 6].Value = salesData[i].Quantity;
                        worksheet.Cells[row, 7].Value = salesData[i].UnitPrice;
                        worksheet.Cells[row, 8].Value = salesData[i].TotalPrice;
                        worksheet.Cells[row, 9].Value = salesData[i].Discount;
                        worksheet.Cells[row, 10].Value = salesData[i].OrderTotal;
                        worksheet.Cells[row, 11].Value = salesData[i].Status;
                    }

                    var bytes = package.GetAsByteArray();
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"BaoCaoBanHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else // CSV
                {
                    var csv = new StringBuilder();
                    // Thêm BOM UTF-8 để Excel đọc đúng tiếng Việt
                    csv.Append('\uFEFF');
                    csv.AppendLine("Ngày,Mã đơn,Khách hàng,Sản phẩm,SKU,Số lượng,Đơn giá,Tổng tiền,Chiết khấu,Tổng đơn,Trạng thái");

                    foreach (var item in salesData)
                    {
                        // Escape dấu phẩy và dấu ngoặc kép trong dữ liệu
                        var customerName = CsvHelper.EscapeCsvField(item.CustomerName ?? "");
                        var productName = CsvHelper.EscapeCsvField(item.ProductName);
                        var sku = CsvHelper.EscapeCsvField(item.Sku ?? "");
                        var status = CsvHelper.EscapeCsvField(item.Status);
                        
                        csv.AppendLine($"{item.Date:yyyy-MM-dd},{item.OrderNumber},{customerName},{productName},{sku},{item.Quantity},{item.UnitPrice},{item.TotalPrice},{item.Discount},{item.OrderTotal},{status}");
                    }

                    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    return File(bytes, "text/csv; charset=utf-8", $"BaoCaoBanHang_{DateTime.Now:yyyyMMddHHmmss}.csv");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/reports/export-inventory?format=csv
        [HttpGet("export-inventory")]
        public async Task<IActionResult> ExportInventoryReport([FromQuery] string format = "csv")
        {
            try
            {
                var inventoryData = await _reportsService.GetInventoryReportAsync();

                if (format.ToLower() == "xlsx")
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Báo cáo tồn kho");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Sản phẩm";
                    worksheet.Cells[1, 2].Value = "SKU";
                    worksheet.Cells[1, 3].Value = "Danh mục";
                    worksheet.Cells[1, 4].Value = "Số lượng";
                    worksheet.Cells[1, 5].Value = "Giá vốn";
                    worksheet.Cells[1, 6].Value = "Tổng giá trị";
                    worksheet.Cells[1, 7].Value = "Giá bán";
                    worksheet.Cells[1, 8].Value = "Cập nhật";

                    // Data
                    for (int i = 0; i < inventoryData.Count; i++)
                    {
                        var row = i + 2;
                        worksheet.Cells[row, 1].Value = inventoryData[i].ProductName;
                        worksheet.Cells[row, 2].Value = inventoryData[i].Sku;
                        worksheet.Cells[row, 3].Value = inventoryData[i].CategoryName;
                        worksheet.Cells[row, 4].Value = inventoryData[i].Quantity;
                        worksheet.Cells[row, 5].Value = inventoryData[i].UnitCost;
                        worksheet.Cells[row, 6].Value = inventoryData[i].TotalValue;
                        worksheet.Cells[row, 7].Value = inventoryData[i].UnitPrice;
                        worksheet.Cells[row, 8].Value = inventoryData[i].LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    var bytes = package.GetAsByteArray();
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"BaoCaoTonKho_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else // CSV
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Sản phẩm,SKU,Danh mục,Số lượng,Giá vốn,Tổng giá trị,Giá bán,Cập nhật");

                    foreach (var item in inventoryData)
                    {
                        csv.AppendLine($"{item.ProductName},{item.Sku ?? ""},{item.CategoryName ?? ""},{item.Quantity},{item.UnitCost},{item.TotalValue},{item.UnitPrice},{item.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}");
                    }

                    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    return File(bytes, "text/csv", $"BaoCaoTonKho_{DateTime.Now:yyyyMMddHHmmss}.csv");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

