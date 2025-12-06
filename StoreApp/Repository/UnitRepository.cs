using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class UnitRepository
    {
        private readonly AppDbContext _context;

        public UnitRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả units (có thể filter theo isActive)
        public async Task<List<Unit>> GetAllAsync(bool? isActive = null)
        {
            var query = _context.Units.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return await query
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        // Lấy unit theo ID
        public async Task<Unit?> GetByIdAsync(int id)
        {
            return await _context.Units
                .Include(u => u.Products)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Lấy unit theo code
        public async Task<Unit?> GetByCodeAsync(string code)
        {
            return await _context.Units
                .FirstOrDefaultAsync(u => u.Code == code);
        }

        // Tạo mới unit
        public async Task<Unit> CreateAsync(Unit unit)
        {
            unit.CreatedAt = DateTime.UtcNow;
            unit.UpdatedAt = DateTime.UtcNow;

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            return unit;
        }

        // Cập nhật unit
        public async Task<Unit?> UpdateAsync(int id, Unit unitUpdate)
        {
            var existing = await _context.Units.FindAsync(id);
            if (existing == null) return null;

            existing.Code = unitUpdate.Code;
            existing.Name = unitUpdate.Name;
            existing.IsActive = unitUpdate.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        // Xóa unit (nếu không có sản phẩm nào dùng)
        public async Task<bool> DeleteAsync(int id)
        {
            var unit = await _context.Units
                .Include(u => u.Products)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return false;

            unit.IsActive = !unit.IsActive;
            unit.UpdatedAt = DateTime.UtcNow;

            _context.Units.Update(unit);

            await _context.SaveChangesAsync();
            return true;
        }

        // Đếm số sản phẩm theo unit
        public async Task<Dictionary<int, int>> GetProductCountByUnitAsync()
        {
            return await _context.Products
                .Where(p => p.UnitId.HasValue)
                .GroupBy(p => p.UnitId!.Value)
                .Select(g => new { UnitId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UnitId, x => x.Count);
        }
    }
}