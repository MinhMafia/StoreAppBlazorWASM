using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class CategoryPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public CategoryPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Lấy danh sách danh mục sản phẩm")]
        public async Task<object> QueryCategories(
            [Description("Tìm theo tên danh mục")] string? keyword = null,
            [Description("Lọc theo trạng thái")] bool? isActive = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 50)
        {
            limit = Math.Min(limit, 100);

            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

            string? status = isActive.HasValue ? (isActive.Value ? "active" : "inactive") : null;

            var result = await categoryService.GetFilteredAndPaginatedAsync(
                page: page,
                pageSize: limit,
                keyword: keyword,
                status: status
            );

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                categories = result.Items.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    Status = c.IsActive ? "active" : "inactive"
                })
            };
        }
    }
}
