using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;
using StoreApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace StoreApp.Services
{
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepo;
        private readonly AppDbContext _context;
        private readonly ImportReceiptService _importReceiptService;

        public InventoryService(
            InventoryRepository inventoryRepo,
            AppDbContext context,
            ImportReceiptService importReceiptService)
        {
            _inventoryRepo = inventoryRepo;
            _context = context;
            _importReceiptService = importReceiptService;
        }

        public async Task<bool> ReduceInventoryAsync(List<ReduceInventoryDto> items)
        {
            var inventoriesToUpdate = new List<Inventory>();

            foreach (var item in items)
            {
                var inventory = await _inventoryRepo.GetByProductIdAsync(item.ProductId);

                if (inventory == null)
                {
                    // Nếu không tìm thấy inventory, bỏ qua hoặc tạo mới
                    // Tùy vào logic nghiệp vụ, có thể throw exception hoặc tạo mới
                    continue;
                }

                inventory.Quantity = inventory.Quantity - item.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                inventoriesToUpdate.Add(inventory);
            }

            // Cập nhật tất cả cùng lúc
            await _inventoryRepo.UpdateRangeAsync(inventoriesToUpdate);
            return true;
        }

        public Task<PaginationResult<InventoryListItemDTO>> GetInventoryPagedAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? stockStatus)
        {
            return _inventoryRepo.GetInventoryPagedAsync(page, pageSize, search, sortBy, stockStatus);
        }

        public Task<InventoryStatsDTO> GetInventoryStatsAsync(int lowStockThreshold = 10)
        {
            return _inventoryRepo.GetInventoryStatsAsync(lowStockThreshold);
        }

        /// <summary>
        /// Điều chỉnh tồn kho thủ công (set quantity mới) và tùy chọn cập nhật cost, ghi log inventory_adjustments.
        /// </summary>
        public async Task AdjustInventoryAsync(
            int inventoryId,
            int newQuantity,
            string? reason,
            ClaimsPrincipal? user,
            decimal? newCost = null,
            int? productId = null)
        {
            if (newQuantity < 0)
                throw new ArgumentException("Số lượng phải >= 0", nameof(newQuantity));

            var inventory = await _context.Inventory
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
                throw new InvalidOperationException("Không tìm thấy bản ghi tồn kho.");

            var oldQuantity = inventory.Quantity;
            if (oldQuantity == newQuantity && !newCost.HasValue)
            {
                // Không có thay đổi, bỏ qua
                return;
            }

            var changeAmount = newQuantity - oldQuantity;

            inventory.Quantity = newQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            // Cập nhật cost của product nếu có
            if (newCost.HasValue && newCost.Value >= 0 && inventory.Product != null)
            {
                inventory.Product.Cost = newCost.Value;
                inventory.Product.UpdatedAt = DateTime.UtcNow;
            }

            if (changeAmount > 0 && inventory.Product != null)
            {
                inventory.Product.IsActive = false;
                inventory.Product.UpdatedAt = DateTime.UtcNow;
            }

            var userId = GetUserId(user);

            var reasonText = string.IsNullOrWhiteSpace(reason)
                ? $"Điều chỉnh thủ công từ {oldQuantity} -> {newQuantity}"
                : reason;

            if (newCost.HasValue)
            {
                reasonText += $" | Cập nhật giá vốn: {newCost.Value:N0}₫";
            }

            var adjustment = new InventoryAdjustment
            {
                ProductId = inventory.ProductId,
                ChangeAmount = changeAmount,
                Reason = reasonText,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            // TỰ ĐỘNG TẠO PHIẾU NHẬP KHI NHẬP HÀNG (changeAmount > 0)
            if (changeAmount > 0 && inventory.Product != null)
            {
                try
                {
                    var createImportDto = new CreateImportDTO
                    {
                        SupplierId = inventory.Product.SupplierId,
                        StaffId = userId,
                        Note = $"Phiếu nhập tự động từ chức năng nhập hàng. {reasonText}",
                        Items = new List<CreateImportItemDTO>
                        {
                            new CreateImportItemDTO
                            {
                                ProductId = inventory.ProductId,
                                Quantity = changeAmount,
                                UnitCost = newCost ?? inventory.Product.Cost ?? 0
                            }
                        }
                    };

                    await _importReceiptService.CreateImportAsync(createImportDto);
                }
                catch (Exception ex)
                {
                    // Log error nhưng không throw để không ảnh hưởng đến việc nhập hàng
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo phiếu nhập tự động: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Kiểm kê và lấy danh sách sản phẩm có vấn đề về giá
        /// </summary>
        public async Task<InventoryAuditResultDTO> AuditInventoryAsync(bool autoDeactivate = false)
        {
            var invalidProducts = await _inventoryRepo.GetInvalidPriceProductsAsync();

            int deactivatedCount = 0;
            if (autoDeactivate && invalidProducts.Any(p => p.IsActive))
            {
                deactivatedCount = await _inventoryRepo.DeactivateInvalidPriceProductsAsync();

                // Reload data sau khi deactivate
                invalidProducts = await _inventoryRepo.GetInvalidPriceProductsAsync();
            }

            return new InventoryAuditResultDTO
            {
                InvalidProducts = invalidProducts,
                TotalInvalid = invalidProducts.Count,
                TotalDeactivated = deactivatedCount
            };
        }

        private int? GetUserId(ClaimsPrincipal? user)
        {
            if (user == null) return null;
            var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }
}