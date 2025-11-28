using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class CategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<(List<Category> Items, int TotalItems)> GetPaginatedAsync(int page, int pageSize, string? search)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(s));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return false;

            _context.Categories.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var q = _context.Categories.AsQueryable().Where(c => c.Name == name);
            if (excludeId.HasValue)
                q = q.Where(c => c.Id != excludeId.Value);

            return await q.AnyAsync();
        }

        // Lấy category theo slug
        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        // Kiểm tra tồn tại theo Id
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }

        // Đếm tổng số categories
        public async Task<int> CountAsync()
        {
            return await _context.Categories.CountAsync();
        }

        // Lấy categories có phân trang và filter
        public async Task<List<Category>> FindCategoriesByFilteredAndPaginatedAsync(
            int skip,
            int pageSize,
            string? keyword = null,
            string? status = null)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(c =>
                    c.Name.Contains(trimmedKeyword) ||
                    (c.Description != null && c.Description.Contains(trimmedKeyword)) ||
                    (c.Slug != null && c.Slug.Contains(trimmedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(c => c.IsActive);
                }
                else if (status.ToLower() == "inactive")
                {
                    query = query.Where(c => !c.IsActive);
                }
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        // Đếm categories với filter
        public async Task<int> CountFilteredAndPaginatedAsync(
            string? keyword = null,
            string? status = null)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(c =>
                    c.Name.Contains(trimmedKeyword) ||
                    (c.Description != null && c.Description.Contains(trimmedKeyword)) ||
                    (c.Slug != null && c.Slug.Contains(trimmedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(c => c.IsActive);
                }
                else if (status.ToLower() == "inactive")
                {
                    query = query.Where(c => !c.IsActive);
                }
            }

            return await query.CountAsync();
        }
    }
}
