using StoreApp.Models;
using StoreApp.Shared;
using StoreApp.Repository;

namespace StoreApp.Services
{
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepo;

        public InventoryService(InventoryRepository inventoryRepo)
        {
            _inventoryRepo = inventoryRepo;
        }

        public async Task<bool> ReduceInventoryAsync(List<ReduceInventoryDto> items)
        {
            var inventoriesToUpdate = new List<Inventory>();

            foreach (var item in items)
            {
                var inventory = await _inventoryRepo.GetByProductIdAsync(item.ProductId);

                inventory.Quantity = inventory.Quantity - item.Quantity;
                inventory.UpdatedAt = DateTime.Now;
                inventoriesToUpdate.Add(inventory);
            }

            // Cập nhật tất cả cùng lúc
            await _inventoryRepo.UpdateRangeAsync(inventoriesToUpdate);
            return true;
        }
    }
}
