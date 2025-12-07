using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class PromotionPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public PromotionPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Tìm kiếm khuyến mãi. Kiểm tra mã cụ thể bằng code.")]
        public async Task<object> QueryPromotions(
            [Description("Tìm theo tên khuyến mãi")] string? keyword = null,
            [Description("Mã khuyến mãi cụ thể")] string? code = null,
            [Description("Trạng thái: active, inactive, expired")] string? status = null,
            [Description("Loại giảm giá: percent, fixed")] string? type = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 20)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<PromotionService>();

            if (!string.IsNullOrEmpty(code))
            {
                var promotion = await promotionService.GetPromotionByCodeAsync(code);
                if (promotion == null)
                    return new { error = $"Không tìm thấy khuyến mãi với mã '{code}'" };

                return new
                {
                    promotion = new
                    {
                        promotion.Id,
                        promotion.Code,
                        promotion.Description,
                        DiscountType = promotion.Type,
                        DiscountValue = promotion.Value,
                        MinOrderValue = promotion.MinOrderAmount,
                        MaxDiscountAmount = promotion.MaxDiscount,
                        promotion.StartDate,
                        promotion.EndDate,
                        promotion.UsageLimit,
                        promotion.UsedCount,
                        IsActive = promotion.Active,
                        IsExpired = promotion.EndDate < DateTime.UtcNow,
                        RemainingUses = promotion.UsageLimit.HasValue ? promotion.UsageLimit - promotion.UsedCount : null
                    }
                };
            }

            var result = await promotionService.GetPaginatedPromotionsAsync(
                page: page,
                pageSize: limit,
                search: keyword,
                status: status,
                type: type
            );

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                promotions = result.Items.Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Description,
                    DiscountType = p.Type,
                    DiscountValue = p.Value,
                    p.StartDate,
                    p.EndDate,
                    IsActive = p.Active,
                    IsExpired = p.EndDate < DateTime.UtcNow
                })
            };
        }
    }
}
