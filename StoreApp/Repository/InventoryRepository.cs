using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class InventoryRepository
    {
        private readonly AppDbContext _context;

        public InventoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Inventory?> GetByProductIdAsync(int productId)
        {
            return await _context.Inventory
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        //Tạo mới inventory mới khi tạo mới product
        public async Task<Inventory> CreateAsync(Inventory inventory)
        {
            inventory.UpdatedAt = DateTime.UtcNow;
            _context.Inventory.Add(inventory);
            await _context.SaveChangesAsync();
            return inventory;
        }

        public async Task UpdateAsync(Inventory inventory)
        {
            _context.Inventory.Update(inventory);
            await _context.SaveChangesAsync();
        }

        // Cập nhật nhiều inventory cùng lúc
        public async Task UpdateRangeAsync(List<Inventory> inventories)
        {
            foreach (var inv in inventories)
            {
                // Bắt buộc EF Core ghi lại Quantity và UpdatedAt
                _context.Entry(inv).Property(i => i.Quantity).IsModified = true;
                _context.Entry(inv).Property(i => i.UpdatedAt).IsModified = true;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy danh sách tồn kho kèm thông tin sản phẩm, có phân trang + tìm kiếm + lọc theo trạng thái + sắp xếp.
        /// </summary>
        public async Task<PaginationResult<InventoryListItemDTO>> GetInventoryPagedAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortBy = "id",
            string? stockStatus = null)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Inventory
                .Include(i => i.Product)!.ThenInclude(p => p!.Category)
                .Include(i => i.Product)!.ThenInclude(p => p!.Unit)
                .Include(i => i.Product)!.ThenInclude(p => p!.Supplier)
                .AsQueryable();

            // Tìm kiếm theo tên SP, SKU
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(i =>
                    i.Product != null &&
                    (i.Product.ProductName.Contains(s) ||
                     (i.Product.Sku != null && i.Product.Sku.Contains(s))));
            }

            // Lọc theo trạng thái tồn kho
            stockStatus = stockStatus?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(stockStatus))
            {
                query = stockStatus switch
                {
                    "out_of_stock" => query.Where(i => i.Quantity == 0),
                    "low_stock" => query.Where(i => i.Quantity > 0 && i.Quantity < 10),
                    "in_stock" => query.Where(i => i.Quantity >= 10),
                    _ => query
                };
            }

            // Sắp xếp
            sortBy = sortBy?.ToLowerInvariant() ?? "id";
            query = sortBy switch
            {
                "quantity_desc" => query.OrderByDescending(i => i.Quantity),
                "quantity_asc" => query.OrderBy(i => i.Quantity),
                "price_desc" => query.OrderByDescending(i => i.Product!.Price),
                "price_asc" => query.OrderBy(i => i.Product!.Price),
                "product_name_asc" => query.OrderBy(i => i.Product!.ProductName),
                "product_name_desc" => query.OrderByDescending(i => i.Product!.ProductName),
                "updated_at_desc" => query.OrderByDescending(i => i.UpdatedAt),
                "updated_at_asc" => query.OrderBy(i => i.UpdatedAt),
                "id_desc" => query.OrderByDescending(i => i.Id),
                _ => query.OrderBy(i => i.Id),
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtoItems = items.Select(i => new InventoryListItemDTO
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? string.Empty,
                Sku = i.Product?.Sku,
                CategoryName = i.Product?.Category?.Name,
                SupplierName = i.Product?.Supplier?.Name,
                Price = i.Product?.Price ?? 0,
                Cost = i.Product?.Cost,
                Unit = i.Product?.Unit?.Name,
                Quantity = i.Quantity,
                UpdatedAt = i.UpdatedAt
            }).ToList();

            return new PaginationResult<InventoryListItemDTO>
            {
                Items = dtoItems,
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };
        }

        /// <summary>
        /// Thống kê nhanh số lượng sản phẩm theo trạng thái tồn kho.
        /// </summary>
        public async Task<InventoryStatsDTO> GetInventoryStatsAsync(int lowStockThreshold = 10)
        {
            var total = await _context.Inventory.CountAsync();
            var outOfStock = await _context.Inventory.CountAsync(i => i.Quantity == 0);
            var lowStock = await _context.Inventory.CountAsync(i => i.Quantity > 0 && i.Quantity < lowStockThreshold);
            var inStock = await _context.Inventory.CountAsync(i => i.Quantity >= lowStockThreshold);

            return new InventoryStatsDTO
            {
                Total = total,
                OutOfStock = outOfStock,
                LowStock = lowStock,
                InStock = inStock
            };
        }

        /// <summary>
        /// Lấy danh sách sản phẩm có giá vốn > giá bán
        /// </summary>
        public async Task<List<InvalidPriceProductDTO>> GetInvalidPriceProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .Where(p => p.Cost.HasValue && p.Cost.Value > p.Price)
                .OrderByDescending(p => p.Cost.Value - p.Price)
                .AsNoTracking()
                .ToListAsync();

            return products.Select(p => new InvalidPriceProductDTO
            {
                ProductId = p.Id,
                ProductName = p.ProductName,
                Sku = p.Sku,
                SupplierName = p.Supplier?.Name,
                Price = p.Price,
                Cost = p.Cost ?? 0,
                PriceDifference = (p.Cost ?? 0) - p.Price,
                Quantity = p.Inventory?.Quantity ?? 0,
                IsActive = p.IsActive
            }).ToList();
        }

        /// <summary>
        /// Ẩn tất cả sản phẩm có giá vốn > giá bán
        /// </summary>
        public async Task<int> DeactivateInvalidPriceProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.Cost.HasValue && p.Cost.Value > p.Price && p.IsActive)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return products.Count;
        }
    }

}

