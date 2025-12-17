using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;

namespace StoreApp.Repository
{
    public class ImportRepository
    {
        private readonly AppDbContext _context;

        public ImportRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách phiếu nhập có phân trang
        /// </summary>
        public async Task<PaginationResult<ImportListItemDTO>> GetImportsPagedAsync(
            int page,
            int pageSize,
            string? search,
            string? status,
            string? sortBy)
        {
            var query = _context.Imports
                .Include(i => i.Supplier)
                .Include(i => i.Staff)
                .Include(i => i.ImportItems)
                .AsQueryable();

            // Filter by search (import_number, supplier name)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.ImportNumber.Contains(search) ||
                    (i.Supplier != null && i.Supplier.Name.Contains(search)));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            // Sorting
            query = sortBy switch
            {
                "amount_desc" => query.OrderByDescending(i => i.TotalAmount),
                "amount_asc" => query.OrderBy(i => i.TotalAmount),
                "created_at_desc" => query.OrderByDescending(i => i.CreatedAt),
                "created_at_asc" => query.OrderBy(i => i.CreatedAt),
                "supplier" => query.OrderBy(i => i.Supplier != null ? i.Supplier.Name : ""),
                _ => query.OrderByDescending(i => i.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var imports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtoItems = imports.Select(i => new ImportListItemDTO
            {
                Id = i.Id,
                ImportNumber = i.ImportNumber,
                SupplierId = i.SupplierId,
                SupplierName = i.Supplier?.Name,
                StaffId = i.StaffId,
                StaffName = i.Staff?.FullName,
                Status = i.Status,
                TotalAmount = i.TotalAmount,
                TotalItems = i.ImportItems.Count,
                TotalQuantity = i.ImportItems.Sum(item => item.Quantity),
                Note = i.Note,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList();

            return new PaginationResult<ImportListItemDTO>
            {
                Items = dtoItems,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết phiếu nhập
        /// </summary>
        public async Task<ImportDetailDTO?> GetImportDetailAsync(int id)
        {
            var import = await _context.Imports
                .Include(i => i.Supplier)
                .Include(i => i.Staff)
                .Include(i => i.ImportItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p!.Unit)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (import == null)
                return null;

            return new ImportDetailDTO
            {
                Id = import.Id,
                ImportNumber = import.ImportNumber,
                SupplierId = import.SupplierId,
                SupplierName = import.Supplier?.Name,
                SupplierPhone = import.Supplier?.Phone,
                SupplierAddress = import.Supplier?.Address,
                StaffId = import.StaffId,
                StaffName = import.Staff?.FullName,
                Status = import.Status,
                TotalAmount = import.TotalAmount,
                Note = import.Note,
                CreatedAt = import.CreatedAt,
                UpdatedAt = import.UpdatedAt,
                Items = import.ImportItems.Select(item => new ImportItemDetailDTO
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.ProductName ?? "",
                    ProductSku = item.Product?.Sku,
                    UnitName = item.Product?.Unit?.Name,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.TotalCost
                }).ToList()
            };
        }

        /// <summary>
        /// Tạo phiếu nhập mới
        /// </summary>
        public async Task<Import> CreateImportAsync(Import import)
        {
            _context.Imports.Add(import);
            await _context.SaveChangesAsync();
            return import;
        }

        /// <summary>
        /// Generate import number (IM + timestamp)
        /// </summary>
        public string GenerateImportNumber()
        {
            return $"IM{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Cập nhật trạng thái phiếu nhập
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var import = await _context.Imports.FindAsync(id);
            if (import == null)
                return false;

            import.Status = status;
            import.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
