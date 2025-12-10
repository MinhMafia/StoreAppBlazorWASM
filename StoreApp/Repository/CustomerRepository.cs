using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class CustomerRepository
    {
        private readonly AppDbContext _context;

        /*************  ✨ Windsurf Command ⭐  *************/
        /// <summary>
        /// Constructor for CustomerRepository.
        /// </summary>
        /// <param name="context">The AppDbContext instance.</param>
        /*******  de317274-e941-4a67-8e12-78d4c12762df  *******/
        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả khách hàng
        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();
        }

        // Lấy khách hàng theo Id
        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Lấy khách hàng theo phone hoặc email
        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        // Lấy khách hàng theo UserId
        public async Task<Customer?> GetByUserIdAsync(int userId)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        // Thêm khách hàng mới
        public async Task<Customer> AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        // Cập nhật khách hàng
        public async Task<Customer> UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        // Xóa khách hàng
        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        // Kiểm tra tồn tại khách hàng theo Id
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Customers.AnyAsync(c => c.Id == id);
        }

        //CODE CŨ
        // public async Task<List<Customer>> GetPaginatedAsync(int page, int pageSize)
        // {
        //     return await _context.Set<Customer>()
        //         .OrderByDescending(c => c.CreatedAt)
        //         .Skip((page - 1) * pageSize)
        //         .Take(pageSize)
        //         .ToListAsync();
        // }
        public async Task<List<Customer>> GetPaginatedAsync(
            int page, int pageSize, string? search = null)
        {
            var query = _context.Set<Customer>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.FullName.Contains(search));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Set<Customer>().CountAsync();
        }

        public async Task<List<Customer>> SearchByNameAsync(string keyword)
        {
            return await _context.Set<Customer>()
                .Where(c => c.FullName.Contains(keyword))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Customer>> findCustomersByFilteredAndPaginatedAsync(
            int skip,
            int pageSize,
            string? keyword = null,
            string? status = null)
        {
            var query = _context.Set<Customer>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(c =>
        c.FullName.Contains(trimmedKeyword) ||
        (c.Phone != null && c.Phone.Contains(trimmedKeyword)) ||
        (c.Email != null && c.Email.Contains(trimmedKeyword)) ||
        (c.Address != null && c.Address.Contains(trimmedKeyword)));
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

        public async Task<int> CountFilteredAndPaginatedAsync(
            string? keyword = null,
            string? status = null)
        {
            var query = _context.Set<Customer>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(c =>
        c.FullName.Contains(trimmedKeyword) ||
        (c.Phone != null && c.Phone.Contains(trimmedKeyword)) ||
        (c.Email != null && c.Email.Contains(trimmedKeyword)) ||
        (c.Address != null && c.Address.Contains(trimmedKeyword)));
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
