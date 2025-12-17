using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;
using StoreApp.Data;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Services
{
    public class ImportReceiptService
    {
        private readonly ImportRepository _importRepo;
        private readonly AppDbContext _context;

        public ImportReceiptService(ImportRepository importRepo, AppDbContext context)
        {
            _importRepo = importRepo;
            _context = context;
        }

        public Task<PaginationResult<ImportListItemDTO>> GetImportsPagedAsync(
            int page,
            int pageSize,
            string? search,
            string? status,
            string? sortBy)
        {
            return _importRepo.GetImportsPagedAsync(page, pageSize, search, status, sortBy);
        }

        public Task<ImportDetailDTO?> GetImportDetailAsync(int id)
        {
            return _importRepo.GetImportDetailAsync(id);
        }

        /// <summary>
        /// Tạo phiếu nhập mới từ DTO.
        /// Tự động cập nhật inventory, cost, ẩn sản phẩm và tạo adjustment log.
        /// </summary>
        public async Task<Import> CreateImportAsync(CreateImportDTO dto)
        {
            // VALIDATE ĐẦU VÀO
            if (dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("Phải có ít nhất 1 sản phẩm trong phiếu nhập.");

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException($"Số lượng sản phẩm ID {item.ProductId} phải > 0.");

                if (item.UnitCost < 0)
                    throw new ArgumentException($"Giá vốn sản phẩm ID {item.ProductId} không được âm.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var import = new Import
                {
                    ImportNumber = _importRepo.GenerateImportNumber(),
                    SupplierId = dto.SupplierId,
                    StaffId = dto.StaffId,
                    Status = "completed", // Phiếu nhập tự động hoàn thành
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Tạo import items và cập nhật inventory, cost, status
                foreach (var itemDto in dto.Items)
                {
                    var item = new ImportItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitCost = itemDto.UnitCost,
                        TotalCost = itemDto.Quantity * itemDto.UnitCost,
                        CreatedAt = DateTime.UtcNow
                    };
                    import.ImportItems.Add(item);

                    // CẬP NHẬT INVENTORY
                    var inventory = await _context.Inventory
                        .Include(i => i.Product)
                        .FirstOrDefaultAsync(i => i.ProductId == itemDto.ProductId);

                    if (inventory == null)
                    {
                        // Tạo inventory mới nếu chưa có
                        inventory = new Inventory
                        {
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Inventory.Add(inventory);
                    }
                    else
                    {
                        inventory.Quantity += itemDto.Quantity;
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }

                    // CẬP NHẬT PRODUCT COST
                    var product = await _context.Products.FindAsync(itemDto.ProductId);
                    if (product != null)
                    {
                        product.Cost = itemDto.UnitCost;

                        // ẨN SẢN PHẨM (IsActive = 0) khi nhập hàng
                        // Logic: Sản phẩm vừa nhập chưa có giá bán phù hợp nên cần ẩn đi
                        // Admin sẽ kiểm tra và bật lại sau khi đảm bảo giá bán >= cost * 1.1
                        product.IsActive = false;
                        product.UpdatedAt = DateTime.UtcNow;
                    }

                    // TẠO INVENTORY ADJUSTMENT LOG
                    var reasonText = $"Nhập hàng từ phiếu {import.ImportNumber}";
                    if (dto.SupplierId.HasValue)
                    {
                        var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
                        if (supplier != null)
                            reasonText += $" - NCC: {supplier.Name}";
                    }
                    reasonText += $" | Giá vốn: {itemDto.UnitCost:N0}₫";

                    var adjustment = new InventoryAdjustment
                    {
                        ProductId = itemDto.ProductId,
                        ChangeAmount = itemDto.Quantity,
                        Reason = reasonText,
                        UserId = dto.StaffId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.InventoryAdjustments.Add(adjustment);
                }

                // Tính tổng tiền
                import.TotalAmount = import.ImportItems.Sum(i => i.TotalCost);

                // Lưu import
                var result = await _importRepo.CreateImportAsync(import);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task<bool> UpdateStatusAsync(int id, string status)
        {
            return _importRepo.UpdateStatusAsync(id, status);
        }
    }
}
