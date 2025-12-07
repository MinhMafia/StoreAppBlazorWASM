using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class CustomerPromotionPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomerPromotionPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Kiểm tra mã khuyến mãi có hợp lệ không")]
        public async Task<object> CheckPromotion(
            [Description("Mã khuyến mãi cần kiểm tra")] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new { error = "Vui lòng nhập mã khuyến mãi" };

            using var scope = _serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<PromotionService>();

            var promotion = await promotionService.GetPromotionByCodeAsync(code.Trim());
            if (promotion == null)
                return new { error = $"Không tìm thấy mã khuyến mãi '{code}'" };

            var isExpired = promotion.EndDate < DateTime.UtcNow;
            var isUsedUp = promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit;

            if (!promotion.Active)
                return new { error = "Mã khuyến mãi này đã bị vô hiệu hóa" };
            if (isExpired)
                return new { error = "Mã khuyến mãi này đã hết hạn" };
            if (isUsedUp)
                return new { error = "Mã khuyến mãi này đã hết lượt sử dụng" };

            return new
            {
                valid = true,
                promotion = new
                {
                    promotion.Code,
                    promotion.Description,
                    DiscountType = promotion.Type,
                    DiscountValue = promotion.Value,
                    MinOrderValue = promotion.MinOrderAmount,
                    MaxDiscountAmount = promotion.MaxDiscount,
                    promotion.EndDate
                }
            };
        }
    }
}
