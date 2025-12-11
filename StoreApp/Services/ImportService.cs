using System.Globalization;
using System.Text;
using StoreApp.Data;
using StoreApp.Shared;
using StoreApp.Models;
using StoreApp.Repository;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http;

namespace StoreApp.Services
{
    public class ImportService
    {
        private readonly ProductRepository _productRepo;
        private readonly CustomerRepository _customerRepo;
        private readonly InventoryRepository _inventoryRepo;
        private readonly CategoryRepository? _categoryRepo;
        private readonly SupplierRepository? _supplierRepo;
        private readonly UnitRepository? _unitRepo;
        private readonly AppDbContext _context;

        public ImportService(
            ProductRepository productRepo,
            CustomerRepository customerRepo,
            InventoryRepository inventoryRepo,
            AppDbContext context,
            CategoryRepository? categoryRepo = null,
            SupplierRepository? supplierRepo = null,
            UnitRepository? unitRepo = null)
        {
            _productRepo = productRepo;
            _customerRepo = customerRepo;
            _inventoryRepo = inventoryRepo;
            _context = context;
            _categoryRepo = categoryRepo;
            _supplierRepo = supplierRepo;
            _unitRepo = unitRepo;
        }

        // ========== PRODUCT IMPORT ==========

        public async Task<ImportResultDTO> ImportProductsAsync(IFormFile file)
        {
            var result = new ImportResultDTO();

            // Validate file
            ValidateFile(file, new[] { ".csv", ".xlsx", ".xls" }, 10 * 1024 * 1024); // 10MB max

            // Parse file
            List<ProductImportDTO> rows;
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension == ".csv")
                {
                    rows = ParseCsvFile<ProductImportDTO>(file.OpenReadStream());
                }
                else
                {
                    rows = await ParseExcelFileAsync<ProductImportDTO>(file.OpenReadStream());
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "File",
                    Message = $"Lỗi đọc file: {ex.Message}"
                });
                return result;
            }

            result.TotalRows = rows.Count;

            if (rows.Count == 0)
            {
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "File",
                    Message = "File không chứa dữ liệu"
                });
                return result;
            }

            // Process rows in transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNumber = i + 2; // +2 vì có header và index bắt đầu từ 0

                    try
                    {
                        // Validate row
                        var validationErrors = await ValidateProductRowAsync(row, rowNumber);
                        if (validationErrors.Any())
                        {
                            result.Errors.AddRange(validationErrors);
                            result.Skipped++;
                            continue;
                        }

                        // Upsert
                        Product? existing = null;
                        if (!string.IsNullOrWhiteSpace(row.Sku))
                        {
                            existing = await _productRepo.GetBySkuAsync(row.Sku);
                        }

                        if (existing != null)
                        {
                            // Update existing product
                            UpdateProductFromImport(existing, row);
                            await _productRepo.UpdateAsync(existing);
                            result.Updated++;
                        }
                        else
                        {
                            // Create new product
                            var product = MapToProduct(row);
                            var created = await _productRepo.CreateAsync(product);

                            // Create inventory record
                            var inventory = new Inventory
                            {
                                ProductId = created.Id,
                                Quantity = 0,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _inventoryRepo.CreateAsync(inventory);

                            result.Created++;
                        }
                    }
                    catch (Exception rowEx)
                    {
                        // Log error for this specific row
                        var errorMessage = GetDetailedErrorMessage(rowEx);
                        result.Errors.Add(new ImportErrorDTO
                        {
                            RowNumber = rowNumber,
                            Field = "System",
                            Message = $"Lỗi ở dòng {rowNumber}: {errorMessage}"
                        });
                        result.Skipped++;
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = GetDetailedErrorMessage(ex);
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "System",
                    Message = $"Lỗi hệ thống: {errorMessage}"
                });
            }

            return result;
        }

        // ========== CUSTOMER IMPORT ==========

        public async Task<ImportResultDTO> ImportCustomersAsync(IFormFile file)
        {
            var result = new ImportResultDTO();

            // Validate file
            ValidateFile(file, new[] { ".csv", ".xlsx", ".xls" }, 10 * 1024 * 1024);

            // Parse file
            List<CustomerImportDTO> rows;
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension == ".csv")
                {
                    rows = ParseCsvFile<CustomerImportDTO>(file.OpenReadStream());
                }
                else
                {
                    rows = await ParseExcelFileAsync<CustomerImportDTO>(file.OpenReadStream());
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "File",
                    Message = $"Lỗi đọc file: {ex.Message}"
                });
                return result;
            }

            result.TotalRows = rows.Count;

            if (rows.Count == 0)
            {
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "File",
                    Message = "File không chứa dữ liệu"
                });
                return result;
            }

            // Process rows in transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNumber = i + 2;

                    // Validate row
                    var validationErrors = ValidateCustomerRow(row, rowNumber);
                    if (validationErrors.Any())
                    {
                        result.Errors.AddRange(validationErrors);
                        result.Skipped++;
                        continue;
                    }

                    // Upsert: Tìm theo Phone hoặc Email (ưu tiên Phone)
                    Customer? existing = null;
                    if (!string.IsNullOrWhiteSpace(row.Phone))
                    {
                        existing = await _customerRepo.GetByPhoneAsync(row.Phone);
                    }
                    // Nếu không tìm thấy theo Phone, thử tìm theo Email
                    if (existing == null && !string.IsNullOrWhiteSpace(row.Email))
                    {
                        existing = await _customerRepo.GetByEmailAsync(row.Email);
                    }

                    if (existing != null)
                    {
                        // Update existing customer
                        UpdateCustomerFromImport(existing, row);
                        await _customerRepo.UpdateAsync(existing);
                        result.Updated++;
                    }
                    else
                    {
                        // Create new customer
                        var customer = MapToCustomer(row);
                        await _customerRepo.AddAsync(customer);
                        result.Created++;
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add(new ImportErrorDTO
                {
                    RowNumber = 0,
                    Field = "System",
                    Message = $"Lỗi hệ thống: {ex.Message}"
                });
            }

            return result;
        }

        // ========== FILE PARSING ==========

        private List<T> ParseCsvFile<T>(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };
            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<T>().ToList();
        }

        private async Task<List<T>> ParseExcelFileAsync<T>(Stream stream)
        {
            using var package = new ExcelPackage(stream);
            
            // Tìm đúng worksheet - ưu tiên sheet "Products" cho ProductImportDTO
            OfficeOpenXml.ExcelWorksheet? worksheet = null;
            var isProductImport = typeof(T) == typeof(ProductImportDTO);
            
            if (isProductImport)
            {
                // Tìm sheet "Products" cho import sản phẩm
                worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => 
                    ws.Name.Equals("Products", StringComparison.OrdinalIgnoreCase));
                
                // Nếu không tìm thấy, dùng sheet đầu tiên
                worksheet ??= package.Workbook.Worksheets[0];
            }
            else
            {
                // Cho CustomerImportDTO, dùng sheet đầu tiên hoặc sheet "Customers"
                worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => 
                    ws.Name.Equals("Customers", StringComparison.OrdinalIgnoreCase)) 
                    ?? package.Workbook.Worksheets[0];
            }
            
            if (worksheet == null)
                return new List<T>();
            
            var rows = new List<T>();

            if (worksheet.Dimension == null)
                return rows;

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            if (rowCount < 2) // Cần ít nhất header + 1 dòng data
                return rows;

            // Get headers
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(header))
                    headers.Add(header);
            }

            // Map headers to properties
            var properties = typeof(T).GetProperties();
            var propertyMap = new Dictionary<int, System.Reflection.PropertyInfo>();
            for (int col = 1; col <= headers.Count; col++)
            {
                var header = headers[col - 1];
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(header, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals(header.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (property != null)
                    propertyMap[col] = property;
            }

            // Special handling for ProductImportDTO - support name mapping
            // isProductImport đã được khai báo ở trên (dòng 302)
            Dictionary<string, int>? categoryNameToId = null;
            Dictionary<string, int>? supplierNameToId = null;
            Dictionary<string, int>? unitNameToId = null;
            Dictionary<string, int>? unitCodeToId = null;

            if (isProductImport)
            {
                // Pre-load mappings for name-to-id conversion
                if (_categoryRepo != null)
                {
                    var categories = await _categoryRepo.GetAllAsync();
                    categoryNameToId = categories.ToDictionary(c => c.Name.ToLowerInvariant(), c => c.Id);
                }
                if (_supplierRepo != null)
                {
                    var suppliers = await _supplierRepo.GetAllAsync();
                    supplierNameToId = suppliers.ToDictionary(s => s.Name.ToLowerInvariant(), s => s.Id);
                }
                if (_unitRepo != null)
                {
                    var units = await _unitRepo.GetAllAsync(isActive: true);
                    unitNameToId = units.ToDictionary(u => u.Name.ToLowerInvariant(), u => u.Id);
                    unitCodeToId = units.ToDictionary(u => u.Code.ToLowerInvariant(), u => u.Id);
                }
            }

            // Read data rows
            for (int row = 2; row <= rowCount; row++)
            {
                var obj = Activator.CreateInstance<T>();
                bool hasData = false;
                bool hasRequiredField = false; // Check if row has at least one required field

                foreach (var kvp in propertyMap)
                {
                    var col = kvp.Key;
                    var property = kvp.Value;
                    var cellValue = worksheet.Cells[row, col].Value;

                    if (cellValue != null)
                    {
                        var stringValue = cellValue.ToString()?.Trim();
                        
                        // Skip rows that are clearly instructions (for ProductImportDTO)
                        if (isProductImport && !string.IsNullOrEmpty(stringValue))
                        {
                            var lowerValue = stringValue.ToLowerInvariant();
                            if (lowerValue.StartsWith("hướng dẫn") || 
                                lowerValue.StartsWith("ví dụ:") ||
                                lowerValue.StartsWith("1.") || 
                                lowerValue.StartsWith("2.") || 
                                lowerValue.StartsWith("3.") || 
                                lowerValue.StartsWith("4.") ||
                                lowerValue.Contains("có thể nhập") ||
                                lowerValue.Contains("nhập số"))
                            {
                                // This is an instruction row, skip it
                                hasData = false;
                                hasRequiredField = false;
                                break;
                            }
                        }

                        hasData = true;
                        
                        // Check if this is a required field (for ProductImportDTO: ProductName)
                        // Only consider it valid if ProductName has actual content (not just whitespace)
                        if (isProductImport && property.Name == "ProductName")
                        {
                            if (!string.IsNullOrWhiteSpace(stringValue) && stringValue.Length >= 2)
                            {
                                hasRequiredField = true;
                            }
                        }
                        
                        try
                        {
                            object? value = null;
                            var propertyName = property.Name;

                            // Special handling for ProductImportDTO - map names to IDs before conversion
                            if (isProductImport && obj is ProductImportDTO productDto)
                            {
                                // Map CategoryName to CategoryId
                                if (propertyName == "CategoryId" && categoryNameToId != null && !string.IsNullOrEmpty(stringValue))
                                {
                                    if (int.TryParse(stringValue, out var categoryId))
                                    {
                                        value = categoryId; // Already an ID
                                    }
                                    else if (categoryNameToId.TryGetValue(stringValue.ToLowerInvariant(), out var mappedCategoryId))
                                    {
                                        value = mappedCategoryId; // Map name to ID
                                    }
                                }
                                // Map SupplierName to SupplierId
                                else if (propertyName == "SupplierId" && supplierNameToId != null && !string.IsNullOrEmpty(stringValue))
                                {
                                    if (int.TryParse(stringValue, out var supplierId))
                                    {
                                        value = supplierId; // Already an ID
                                    }
                                    else if (supplierNameToId.TryGetValue(stringValue.ToLowerInvariant(), out var mappedSupplierId))
                                    {
                                        value = mappedSupplierId; // Map name to ID
                                    }
                                }
                                // Map UnitName or UnitCode to UnitId
                                else if (propertyName == "UnitId" && (unitNameToId != null || unitCodeToId != null) && !string.IsNullOrEmpty(stringValue))
                                {
                                    if (int.TryParse(stringValue, out var unitId))
                                    {
                                        value = unitId; // Already an ID
                                    }
                                    else if (unitNameToId != null && unitNameToId.TryGetValue(stringValue.ToLowerInvariant(), out var mappedUnitId))
                                    {
                                        value = mappedUnitId; // Map name to ID
                                    }
                                    else if (unitCodeToId != null && unitCodeToId.TryGetValue(stringValue.ToLowerInvariant(), out var mappedUnitIdByCode))
                                    {
                                        value = mappedUnitIdByCode; // Map code to ID
                                    }
                                }
                                else
                                {
                                    // Normal conversion for other fields
                                    value = ConvertValue(cellValue, property.PropertyType);
                                }
                            }
                            else
                            {
                                // Normal conversion for non-ProductImportDTO
                                value = ConvertValue(cellValue, property.PropertyType);
                            }
                            
                            if (value != null)
                                property.SetValue(obj, value);
                        }
                        catch
                        {
                            // Ignore conversion errors, will be caught in validation
                        }
                    }
                }

                // Only add row if it has data AND at least one required field (for ProductImportDTO: ProductName)
                if (hasData && (isProductImport ? hasRequiredField : true))
                {
                    rows.Add(obj);
                }
            }

            return rows;
        }

        private object? ConvertValue(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
                return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
                return value.ToString();

            if (underlyingType == typeof(decimal) || underlyingType == typeof(decimal?))
            {
                if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    return dec;
            }

            if (underlyingType == typeof(int) || underlyingType == typeof(int?))
            {
                if (int.TryParse(value.ToString(), out var i))
                    return i;
            }

            if (underlyingType == typeof(bool) || underlyingType == typeof(bool?))
            {
                if (bool.TryParse(value.ToString(), out var b))
                    return b;
                // Handle "1"/"0", "yes"/"no"
                var str = value.ToString()?.ToLowerInvariant();
                if (str == "1" || str == "yes" || str == "true" || str == "có")
                    return true;
                if (str == "0" || str == "no" || str == "false" || str == "không")
                    return false;
            }

            return Convert.ChangeType(value, underlyingType);
        }

        // ========== VALIDATION ==========

        private async Task<List<ImportErrorDTO>> ValidateProductRowAsync(ProductImportDTO row, int rowNumber)
        {
            var errors = new List<ImportErrorDTO>();

            if (string.IsNullOrWhiteSpace(row.ProductName))
            {
                errors.Add(new ImportErrorDTO
                {
                    RowNumber = rowNumber,
                    Field = nameof(row.ProductName),
                    Message = "Tên sản phẩm là bắt buộc"
                });
            }

            if (row.Price <= 0)
            {
                errors.Add(new ImportErrorDTO
                {
                    RowNumber = rowNumber,
                    Field = nameof(row.Price),
                    Message = "Giá sản phẩm phải lớn hơn 0"
                });
            }

            // Validate CategoryId exists (if provided)
            if (row.CategoryId.HasValue)
            {
                if (row.CategoryId.Value <= 0)
                {
                    errors.Add(new ImportErrorDTO
                    {
                        RowNumber = rowNumber,
                        Field = nameof(row.CategoryId),
                        Message = "CategoryId phải lớn hơn 0"
                    });
                }
                else if (_categoryRepo != null)
                {
                    var categoryExists = await _categoryRepo.GetByIdAsync(row.CategoryId.Value);
                    if (categoryExists == null)
                    {
                        errors.Add(new ImportErrorDTO
                        {
                            RowNumber = rowNumber,
                            Field = nameof(row.CategoryId),
                            Message = $"CategoryId {row.CategoryId.Value} không tồn tại trong hệ thống"
                        });
                    }
                }
            }

            // Validate SupplierId exists (if provided)
            if (row.SupplierId.HasValue)
            {
                if (row.SupplierId.Value <= 0)
                {
                    errors.Add(new ImportErrorDTO
                    {
                        RowNumber = rowNumber,
                        Field = nameof(row.SupplierId),
                        Message = "SupplierId phải lớn hơn 0"
                    });
                }
                else if (_supplierRepo != null)
                {
                    var supplierExists = await _supplierRepo.GetByIdAsync(row.SupplierId.Value);
                    if (supplierExists == null)
                    {
                        errors.Add(new ImportErrorDTO
                        {
                            RowNumber = rowNumber,
                            Field = nameof(row.SupplierId),
                            Message = $"SupplierId {row.SupplierId.Value} không tồn tại trong hệ thống"
                        });
                    }
                }
            }

            // Validate UnitId exists (if provided)
            if (row.UnitId.HasValue)
            {
                if (row.UnitId.Value <= 0)
                {
                    errors.Add(new ImportErrorDTO
                    {
                        RowNumber = rowNumber,
                        Field = nameof(row.UnitId),
                        Message = "UnitId phải lớn hơn 0"
                    });
                }
                else if (_unitRepo != null)
                {
                    var unitExists = await _unitRepo.GetByIdAsync(row.UnitId.Value);
                    if (unitExists == null)
                    {
                        errors.Add(new ImportErrorDTO
                        {
                            RowNumber = rowNumber,
                            Field = nameof(row.UnitId),
                            Message = $"UnitId {row.UnitId.Value} không tồn tại trong hệ thống"
                        });
                    }
                }
            }

            return errors;
        }

        private string GetDetailedErrorMessage(Exception ex)
        {
            var message = ex.Message;
            
            // Include inner exception if available
            if (ex.InnerException != null)
            {
                message += $" | Chi tiết: {ex.InnerException.Message}";
            }

            // Check for common database errors
            if (ex.Message.Contains("foreign key constraint") || ex.Message.Contains("FOREIGN KEY"))
            {
                message += " | Lỗi: Khóa ngoại không hợp lệ (CategoryId, SupplierId hoặc UnitId không tồn tại)";
            }
            else if (ex.Message.Contains("unique constraint") || ex.Message.Contains("UNIQUE") || ex.Message.Contains("Duplicate entry"))
            {
                message += " | Lỗi: Dữ liệu trùng lặp (có thể SKU đã tồn tại)";
            }
            else if (ex.Message.Contains("cannot be null") || ex.Message.Contains("NOT NULL"))
            {
                message += " | Lỗi: Thiếu dữ liệu bắt buộc";
            }

            return message;
        }

        private List<ImportErrorDTO> ValidateCustomerRow(CustomerImportDTO row, int rowNumber)
        {
            var errors = new List<ImportErrorDTO>();

            if (string.IsNullOrWhiteSpace(row.FullName))
            {
                errors.Add(new ImportErrorDTO
                {
                    RowNumber = rowNumber,
                    Field = nameof(row.FullName),
                    Message = "Tên khách hàng là bắt buộc"
                });
            }

            if (string.IsNullOrWhiteSpace(row.Phone) && string.IsNullOrWhiteSpace(row.Email))
            {
                errors.Add(new ImportErrorDTO
                {
                    RowNumber = rowNumber,
                    Field = "Phone/Email",
                    Message = "Phải có ít nhất một trong số: Số điện thoại hoặc Email"
                });
            }

            // Validate email format
            if (!string.IsNullOrWhiteSpace(row.Email) && !IsValidEmail(row.Email))
            {
                errors.Add(new ImportErrorDTO
                {
                    RowNumber = rowNumber,
                    Field = nameof(row.Email),
                    Message = "Email không hợp lệ"
                });
            }

            return errors;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ========== MAPPING ==========

        private Product MapToProduct(ProductImportDTO dto)
        {
            return new Product
            {
                Sku = dto.Sku,
                ProductName = dto.ProductName,
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId,
                Price = dto.Price,
                Cost = dto.Cost,
                UnitId = dto.UnitId,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void UpdateProductFromImport(Product product, ProductImportDTO dto)
        {
            product.ProductName = dto.ProductName;
            product.CategoryId = dto.CategoryId;
            product.SupplierId = dto.SupplierId;
            product.Price = dto.Price;
            product.Cost = dto.Cost;
            product.UnitId = dto.UnitId;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
        }

        private Customer MapToCustomer(CustomerImportDTO dto)
        {
            return new Customer
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void UpdateCustomerFromImport(Customer customer, CustomerImportDTO dto)
        {
            customer.FullName = dto.FullName;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            customer.Address = dto.Address;
            customer.Note = dto.Note;
            customer.UpdatedAt = DateTime.UtcNow;
        }

        // ========== FILE VALIDATION ==========

        private void ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeBytes)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"Định dạng file không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}");

            if (file.Length > maxSizeBytes)
                throw new ArgumentException($"Kích thước file vượt quá giới hạn {maxSizeBytes / 1024 / 1024}MB");
        }
    }
}

