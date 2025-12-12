using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StoreApp.Services;
using StoreApp.Shared;
using StoreApp.Repository;
using OfficeOpenXml;
using System.Text;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,staff")]
    public class ImportController : ControllerBase
    {
        private readonly ImportService _importService;
        private readonly CategoryRepository _categoryRepo;
        private readonly SupplierRepository _supplierRepo;
        private readonly UnitRepository _unitRepo;

        public ImportController(
            ImportService importService,
            CategoryRepository categoryRepo,
            SupplierRepository supplierRepo,
            UnitRepository unitRepo)
        {
            _importService = importService;
            _categoryRepo = categoryRepo;
            _supplierRepo = supplierRepo;
            _unitRepo = unitRepo;
        }

        // POST api/import/products
        [HttpPost("products")]
        public async Task<ActionResult<ImportResultDTO>> ImportProducts([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không được để trống");

            try
            {
                var result = await _importService.ImportProductsAsync(file);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // POST api/import/customers
        [HttpPost("customers")]
        public async Task<ActionResult<ImportResultDTO>> ImportCustomers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không được để trống");

            try
            {
                var result = await _importService.ImportCustomersAsync(file);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        // GET api/import/template/products?format=csv|excel
        [HttpGet("template/products")]
        public async Task<IActionResult> DownloadProductTemplate([FromQuery] string format = "excel")
        {
            try
            {
                if (format.ToLowerInvariant() == "csv")
                {
                    return GenerateProductCsvTemplate();
                }
                else
                {
                    return await GenerateProductExcelTemplate();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo template: {ex.Message}" });
            }
        }

        // GET api/import/template/customers?format=csv|excel
        [HttpGet("template/customers")]
        public IActionResult DownloadCustomerTemplate([FromQuery] string format = "excel")
        {
            try
            {
                if (format.ToLowerInvariant() == "csv")
                {
                    return GenerateCustomerCsvTemplate();
                }
                else
                {
                    return GenerateCustomerExcelTemplate();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo template: {ex.Message}" });
            }
        }

        // ========== TEMPLATE GENERATORS ==========

        private async Task<IActionResult> GenerateProductExcelTemplate()
        {
            using var package = new ExcelPackage();
            
            // Sheet 1: Hướng dẫn (Instructions) - Đặt đầu tiên để dễ tìm
            var instructionsSheet = package.Workbook.Worksheets.Add("Hướng dẫn");
            instructionsSheet.Cells[1, 1].Value = "HƯỚNG DẪN NHẬP DỮ LIỆU SẢN PHẨM";
            instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[1, 1].Style.Font.Size = 14;
            instructionsSheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            
            instructionsSheet.Cells[3, 1].Value = "1. CategoryId, SupplierId, UnitId:";
            instructionsSheet.Cells[3, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[3, 2].Value = "Có thể nhập ID (số) HOẶC tên/code (xem các sheet tham khảo bên dưới)";
            instructionsSheet.Cells[4, 2].Value = "Ví dụ: CategoryId = 1 HOẶC CategoryId = 'Đồ uống' đều được";
            instructionsSheet.Cells[4, 2].Style.Font.Italic = true;
            instructionsSheet.Cells[4, 2].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            
            instructionsSheet.Cells[6, 1].Value = "2. Price và Cost:";
            instructionsSheet.Cells[6, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[6, 2].Value = "Nhập số (không có dấu phẩy, dấu chấm hoặc ký tự đặc biệt)";
            instructionsSheet.Cells[7, 2].Value = "Ví dụ: 100000 (đúng) hoặc 100.000 (sai)";
            instructionsSheet.Cells[7, 2].Style.Font.Italic = true;
            instructionsSheet.Cells[7, 2].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            
            instructionsSheet.Cells[9, 1].Value = "3. IsActive:";
            instructionsSheet.Cells[9, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[9, 2].Value = "true hoặc false (hoặc 1/0)";
            
            instructionsSheet.Cells[11, 1].Value = "4. Sku:";
            instructionsSheet.Cells[11, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[11, 2].Value = "Để trống nếu muốn tự động tạo, hoặc nhập mã SKU duy nhất";
            
            instructionsSheet.Cells[13, 1].Value = "5. ProductName:";
            instructionsSheet.Cells[13, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[13, 2].Value = "Bắt buộc phải nhập (tối thiểu 2 ký tự)";
            instructionsSheet.Cells[13, 2].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            
            instructionsSheet.Cells[15, 1].Value = "6. Price:";
            instructionsSheet.Cells[15, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[15, 2].Value = "Bắt buộc phải nhập (phải lớn hơn 0)";
            instructionsSheet.Cells[15, 2].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            
            instructionsSheet.Cells[17, 1].Value = "LƯU Ý:";
            instructionsSheet.Cells[17, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[17, 1].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            instructionsSheet.Cells[17, 2].Value = "Chỉ nhập dữ liệu vào sheet 'Products'. Các sheet khác chỉ để tham khảo.";
            
            instructionsSheet.Cells.AutoFitColumns();
            
            // Sheet 2: Products (main data) - Có header, dữ liệu mẫu và hướng dẫn ở bên phải
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Headers - Khớp với form thêm sản phẩm
            // Thứ tự: ProductName (bắt buộc), SKU, UnitId, CategoryId, SupplierId, Price (bắt buộc), Cost, IsActive, Description, ImageUrl
            worksheet.Cells[1, 1].Value = "ProductName";
            worksheet.Cells[1, 2].Value = "Sku";
            worksheet.Cells[1, 3].Value = "UnitId";
            worksheet.Cells[1, 4].Value = "CategoryId";
            worksheet.Cells[1, 5].Value = "SupplierId";
            worksheet.Cells[1, 6].Value = "Price";
            worksheet.Cells[1, 7].Value = "Cost";
            worksheet.Cells[1, 8].Value = "IsActive";
            worksheet.Cells[1, 9].Value = "Description";
            worksheet.Cells[1, 10].Value = "ImageUrl";

            // Sample data - Khớp với thứ tự header mới
            worksheet.Cells[2, 1].Value = "Sản phẩm mẫu"; // ProductName
            worksheet.Cells[2, 2].Value = "SKU001"; // Sku
            worksheet.Cells[2, 3].Value = 1; // UnitId
            worksheet.Cells[2, 4].Value = 1; // CategoryId
            worksheet.Cells[2, 5].Value = 1; // SupplierId
            worksheet.Cells[2, 6].Value = 100000; // Price
            worksheet.Cells[2, 7].Value = 80000; // Cost
            worksheet.Cells[2, 8].Value = true; // IsActive
            worksheet.Cells[2, 9].Value = "Mô tả sản phẩm"; // Description
            worksheet.Cells[2, 10].Value = ""; // ImageUrl

            // Format header
            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Hướng dẫn ở cột L (cách ImageUrl 2 cột: J -> K -> L)
            // ImageUrl là cột 10 (J), cách 2 cột = cột 12 (L)
            int instructionCol = 12; // Cột L
            
            // Tiêu đề hướng dẫn
            worksheet.Cells[1, instructionCol].Value = "HƯỚNG DẪN";
            using (var range = worksheet.Cells[1, instructionCol, 1, instructionCol + 1])
            {
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkBlue);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            }

            // Nội dung hướng dẫn
            int row = 2;
            worksheet.Cells[row, instructionCol].Value = "1. CategoryId, SupplierId, UnitId:";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   Có thể nhập ID (số) HOẶC tên/code";
            worksheet.Cells[row, instructionCol].Style.Font.Size = 10;
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   Ví dụ: 1 hoặc 'Đồ uống'";
            worksheet.Cells[row, instructionCol].Style.Font.Italic = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            row += 2;
            
            worksheet.Cells[row, instructionCol].Value = "2. Price, Cost:";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   Nhập số (không dấu phẩy/chấm)";
            worksheet.Cells[row, instructionCol].Style.Font.Size = 10;
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   Ví dụ: 100000 (đúng)";
            worksheet.Cells[row, instructionCol].Style.Font.Italic = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            row += 2;
            
            worksheet.Cells[row, instructionCol].Value = "3. IsActive:";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   true/false hoặc 1/0";
            worksheet.Cells[row, instructionCol].Style.Font.Size = 10;
            row += 2;
            
            worksheet.Cells[row, instructionCol].Value = "4. Sku:";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   Để trống = tự động tạo";
            worksheet.Cells[row, instructionCol].Style.Font.Size = 10;
            row += 2;
            
            worksheet.Cells[row, instructionCol].Value = "5. ProductName, Price:";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            row++;
            
            worksheet.Cells[row, instructionCol].Value = "   BẮT BUỘC phải nhập";
            worksheet.Cells[row, instructionCol].Style.Font.Bold = true;
            worksheet.Cells[row, instructionCol].Style.Font.Color.SetColor(System.Drawing.Color.Red);
            worksheet.Cells[row, instructionCol].Style.Font.Size = 10;

            // Tô màu nền cho toàn bộ vùng hướng dẫn
            using (var range = worksheet.Cells[1, instructionCol, row, instructionCol + 1])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.DarkBlue);
            }

            // Merge các ô hướng dẫn để dễ đọc
            for (int r = 2; r <= row; r++)
            {
                worksheet.Cells[r, instructionCol, r, instructionCol + 1].Merge = true;
                worksheet.Cells[r, instructionCol].Style.WrapText = true;
                worksheet.Cells[r, instructionCol].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
            }

            worksheet.Cells.AutoFitColumns();

            // Sheet 2: Categories Reference
            var categoriesSheet = package.Workbook.Worksheets.Add("Danh mục (Categories)");
            categoriesSheet.Cells[1, 1].Value = "CategoryId";
            categoriesSheet.Cells[1, 2].Value = "Tên danh mục";
            categoriesSheet.Cells[1, 3].Value = "Trạng thái";
            
            var categories = await _categoryRepo.GetAllAsync();
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                categoriesSheet.Cells[i + 2, 1].Value = cat.Id;
                categoriesSheet.Cells[i + 2, 2].Value = cat.Name;
                categoriesSheet.Cells[i + 2, 3].Value = cat.IsActive ? "Hoạt động" : "Vô hiệu";
            }

            // Format categories header
            using (var range = categoriesSheet.Cells[1, 1, 1, 3])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }
            categoriesSheet.Cells.AutoFitColumns();

            // Sheet 3: Suppliers Reference
            var suppliersSheet = package.Workbook.Worksheets.Add("Nhà cung cấp (Suppliers)");
            suppliersSheet.Cells[1, 1].Value = "SupplierId";
            suppliersSheet.Cells[1, 2].Value = "Tên nhà cung cấp";
            suppliersSheet.Cells[1, 3].Value = "Email";
            suppliersSheet.Cells[1, 4].Value = "Điện thoại";
            suppliersSheet.Cells[1, 5].Value = "Trạng thái";

            var suppliers = await _supplierRepo.GetAllAsync();
            for (int i = 0; i < suppliers.Count; i++)
            {
                var sup = suppliers[i];
                suppliersSheet.Cells[i + 2, 1].Value = sup.Id;
                suppliersSheet.Cells[i + 2, 2].Value = sup.Name;
                suppliersSheet.Cells[i + 2, 3].Value = sup.Email ?? "";
                suppliersSheet.Cells[i + 2, 4].Value = sup.Phone ?? "";
                suppliersSheet.Cells[i + 2, 5].Value = sup.IsActive ? "Hoạt động" : "Vô hiệu";
            }

            // Format suppliers header
            using (var range = suppliersSheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            suppliersSheet.Cells.AutoFitColumns();

            // Sheet 4: Units Reference
            var unitsSheet = package.Workbook.Worksheets.Add("Đơn vị (Units)");
            unitsSheet.Cells[1, 1].Value = "UnitId";
            unitsSheet.Cells[1, 2].Value = "Mã (Code)";
            unitsSheet.Cells[1, 3].Value = "Tên đơn vị";
            unitsSheet.Cells[1, 4].Value = "Trạng thái";

            var units = await _unitRepo.GetAllAsync(isActive: true);
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                unitsSheet.Cells[i + 2, 1].Value = unit.Id;
                unitsSheet.Cells[i + 2, 2].Value = unit.Code;
                unitsSheet.Cells[i + 2, 3].Value = unit.Name;
                unitsSheet.Cells[i + 2, 4].Value = unit.IsActive ? "Hoạt động" : "Vô hiệu";
            }

            // Format units header
            using (var range = unitsSheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }
            unitsSheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "product_template.xlsx");
        }

        private IActionResult GenerateProductCsvTemplate()
        {
            var csv = new StringBuilder();
            // Thứ tự khớp với Excel template
            csv.AppendLine("ProductName,Sku,UnitId,CategoryId,SupplierId,Price,Cost,IsActive,Description,ImageUrl");
            csv.AppendLine("Sản phẩm mẫu,SKU001,1,1,1,100000,80000,true,Mô tả sản phẩm,");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var stream = new MemoryStream(bytes);

            return File(stream, "text/csv", "product_template.csv");
        }

        private IActionResult GenerateCustomerExcelTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Customers");

            // Headers
            worksheet.Cells[1, 1].Value = "FullName";
            worksheet.Cells[1, 2].Value = "Phone";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Address";
            worksheet.Cells[1, 5].Value = "Note";

            // Sample data
            worksheet.Cells[2, 1].Value = "Nguyễn Văn A";
            worksheet.Cells[2, 2].Value = "0123456789";
            worksheet.Cells[2, 3].Value = "nguyenvana@example.com";
            worksheet.Cells[2, 4].Value = "123 Đường ABC, Quận XYZ";
            worksheet.Cells[2, 5].Value = "Khách hàng VIP";

            // Format header
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "customer_template.xlsx");
        }

        private IActionResult GenerateCustomerCsvTemplate()
        {
            var csv = new StringBuilder();
            csv.AppendLine("FullName,Phone,Email,Address,Note");
            csv.AppendLine("Nguyễn Văn A,0123456789,nguyenvana@example.com,123 Đường ABC Quận XYZ,Khách hàng VIP");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var stream = new MemoryStream(bytes);

            return File(stream, "text/csv", "customer_template.csv");
        }
    }
}
