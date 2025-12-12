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

        [KernelFunction, Description("Lấy danh sách khuyến mãi đang hoạt động. Dùng khi khách hỏi 'có khuyến mãi gì?', 'chương trình giảm giá nào đang có?'")]
        public async Task<object> GetActivePromotions(
            [Description("Số lượng kết quả tối đa (mặc định 5, tối đa 10)")] int limit = 5)
        {
            limit = Math.Clamp(limit, 1, 10);

            using var scope = _serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<PromotionService>();

            var activePromotions = await promotionService.GetActivePromotionsAsync();
            
            if (activePromotions == null || !activePromotions.Any())
                return new { message = "Hiện tại không có khuyến mãi nào đang hoạt động" };

            var result = activePromotions.Take(limit).Select(p => new
            {
                p.Code,
                p.Description,
                DiscountType = p.Type,
                DiscountValue = p.Value,
                MinOrderValue = p.MinOrderAmount,
                MaxDiscountAmount = p.MaxDiscount,
                EndDate = p.EndDate?.ToString("dd/MM/yyyy")
            }).ToList();

            return new
            {
                total = result.Count,
                promotions = result
            };
        }

        [KernelFunction, Description("Tìm kiếm khuyến mãi theo từ khóa. Dùng khi khách hỏi 'khuyến mãi về laptop', 'giảm giá điện thoại'")]
        public async Task<object> SearchPromotions(
            [Description("Từ khóa tìm kiếm (tên, mô tả khuyến mãi)")] string keyword,
            [Description("Số lượng kết quả tối đa (mặc định 5, tối đa 10)")] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new { error = "Vui lòng nhập từ khóa tìm kiếm" };

            limit = Math.Clamp(limit, 1, 10);

            using var scope = _serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<PromotionService>();

            var paginatedResult = await promotionService.GetPaginatedPromotionsAsync(
                page: 1,
                pageSize: 20,
                search: keyword.Trim(),
                status: "active"
            );

            if (paginatedResult.Items == null || !paginatedResult.Items.Any())
                return new { message = $"Không tìm thấy khuyến mãi nào với từ khóa '{keyword}'" };

            var result = paginatedResult.Items.Take(limit).Select(p => new
            {
                p.Code,
                p.Description,
                DiscountType = p.Type,
                DiscountValue = p.Value,
                MinOrderValue = p.MinOrderAmount,
                MaxDiscountAmount = p.MaxDiscount,
                EndDate = p.EndDate?.ToString("dd/MM/yyyy")
            }).ToList();

            return new
            {
                keyword,
                total = result.Count,
                promotions = result
            };
        }

        [KernelFunction, Description("Kiểm tra mã khuyến mãi cụ thể có hợp lệ không. Dùng khi khách hỏi 'mã ABC có dùng được không?', 'kiểm tra mã XYZ'")]
        public async Task<object> CheckPromotion(
            [Description("Mã khuyến mãi cần kiểm tra")] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new { error = "Vui lòng nhập mã khuyến mãi" };

            using var scope = _serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<PromotionService>();

            var promotion = await promotionService.GetPromotionByCodeAsync(code.Trim());
            if (promotion == null)
                return new { error = "Mã khuyến mãi không hợp lệ" };

            var now = DateTime.UtcNow;
            var isNotStarted = promotion.StartDate.HasValue && promotion.StartDate > now;
            var isExpired = promotion.EndDate.HasValue && promotion.EndDate < now;
            var isUsedUp = promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit;

            if (!promotion.Active)
                return new { error = "Mã khuyến mãi này đã bị vô hiệu hóa" };
            if (isNotStarted)
                return new { error = "Mã khuyến mãi này chưa đến thời gian sử dụng" };
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
                    EndDate = promotion.EndDate?.ToString("dd/MM/yyyy")
                }
            };
        }
    }
}
