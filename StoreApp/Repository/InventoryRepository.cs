using StoreApp.Data;
using StoreApp.Models;
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

        // Tùy chọn: Cập nhật nhiều inventory cùng lúc
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


    }

}
