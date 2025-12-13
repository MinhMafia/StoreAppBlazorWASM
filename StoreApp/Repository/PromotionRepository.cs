using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
using StoreApp.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class PromotionRepository
    {
        private readonly AppDbContext _context;

        public PromotionRepository(AppDbContext context)
        {
            _context = context;
        }

        // Get all promotions
        public async Task<List<Promotion>> GetAllAsync()
        {
            return await _context.Promotions
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Get paginated promotions with filters
        public async Task<PaginationResult<Promotion>> GetPaginatedAsync(
            int page, 
            int pageSize, 
            string? search = null, 
            string? status = null, 
            string? type = null)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            // Sử dụng DateTimeHelper để thống nhất timezone
            var vnToday = DateTimeHelper.VietnamToday;
            IQueryable<Promotion> query = _context.Promotions
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            // Search by code or description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p => 
                    p.Code.ToLower().Contains(searchLower) || 
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                switch (status.ToLower())
                {
                    case "active":
                        // "active" trong màn quản lý = đang chạy (theo ngày VN)
                        query = query.Where(p =>
                            p.Active &&
                            (!p.StartDate.HasValue || p.StartDate.Value.Date <= vnToday) &&
                            (!p.EndDate.HasValue || p.EndDate.Value.Date >= vnToday) &&
                            (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit));
                        break;
                    case "inactive":
                        query = query.Where(p => !p.Active);
                        break;
                    case "expired":
                        // Hết hạn theo ngày VN
                        query = query.Where(p => p.EndDate.HasValue && p.EndDate.Value.Date < vnToday);
                        break;
                    case "scheduled":
                        // "scheduled" = đã bật nhưng chưa tới ngày bắt đầu (theo ngày VN)
                        query = query.Where(p =>
                            p.Active &&
                            p.StartDate.HasValue && p.StartDate.Value.Date > vnToday &&
                            (!p.EndDate.HasValue || p.EndDate.Value.Date >= vnToday) &&
                            (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit));
                        break;
                }
            }

            // Filter by type
            if (!string.IsNullOrWhiteSpace(type) && type != "all")
            {
                query = query.Where(p => p.Type == type);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResult<Promotion>
            {
                Items = items,
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };
        }

        // Get promotion by ID
        public async Task<Promotion?> GetByIdAsync(int id)
        {
            return await _context.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        // Get promotion by code
        public async Task<Promotion?> GetByCodeAsync(string code)
        {
            return await _context.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code && !p.IsDeleted);
        }

        // Create promotion
        public async Task<Promotion> CreateAsync(Promotion promotion)
        {
            promotion.CreatedAt = DateTimeHelper.UtcNow;
            promotion.UpdatedAt = DateTimeHelper.UtcNow;

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        // Update promotion
        public async Task<Promotion> UpdateAsync(Promotion promotion)
        {
            promotion.UpdatedAt = DateTimeHelper.UtcNow;
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        // Soft delete promotion
        public async Task<bool> DeleteAsync(int id)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null) return false;

            promotion.IsDeleted = true;
            promotion.DeletedAt = DateTimeHelper.UtcNow;
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
            return true;
        }

        // Get active promotions
        public async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            // Sử dụng DateTimeHelper để thống nhất timezone
            var vnToday = DateTimeHelper.VietnamToday;
            return await _context.Promotions
                .AsNoTracking()
                .Where(p => !p.IsDeleted &&
                           p.Active &&
                           (p.StartDate == null || p.StartDate.Value.Date <= vnToday) &&
                           (p.EndDate == null || p.EndDate.Value.Date >= vnToday) &&
                           (p.UsageLimit == null || p.UsedCount < p.UsageLimit))
                .ToListAsync();
        }

        // Get redemptions for a promotion
        public async Task<List<PromotionRedemption>> GetRedemptionsAsync(int promotionId)
        {
            return await _context.PromotionRedemptions
                .AsNoTracking()
                .Where(pr => pr.PromotionId == promotionId)
                .Include(pr => pr.Customer)
                .Include(pr => pr.Order)
                .OrderByDescending(pr => pr.RedeemedAt)
                .ToListAsync();
        }

        // Increment used count
        public async Task<bool> IncrementUsedCountAsync(int promotionId)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion == null) return false;

            promotion.UsedCount++;
            promotion.UpdatedAt = DateTimeHelper.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // Add redemption record
        public async Task<PromotionRedemption> AddRedemptionAsync(PromotionRedemption redemption)
        {
            redemption.RedeemedAt = DateTimeHelper.UtcNow;
            _context.PromotionRedemptions.Add(redemption);
            await _context.SaveChangesAsync();
            return redemption;
        }

        /// <summary>
        /// Áp dụng khuyến mãi cho một đơn hàng đã tạo (alternative method for POS)
        /// </summary>
        public async Task ApplyPromotionAsync(Order order)
        {
            if (!order.PromotionId.HasValue) return;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var promo = await GetByIdAsync(order.PromotionId.Value);
                if (promo == null)
                    throw new Exception("Khuyến mãi không tồn tại");

                if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
                    throw new Exception("Khuyến mãi đã hết lượt sử dụng");

                promo.UsedCount += 1;
                _context.Promotions.Update(promo);

                var redemption = new PromotionRedemption
                {
                    PromotionId = promo.Id,
                    CustomerId = order.CustomerId,
                    OrderId = order.Id,
                    RedeemedAt = DateTimeHelper.UtcNow
                };
                await _context.PromotionRedemptions.AddAsync(redemption);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}