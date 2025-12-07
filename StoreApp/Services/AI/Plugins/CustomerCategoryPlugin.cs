using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class CustomerCategoryPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomerCategoryPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Xem danh sách danh mục sản phẩm")]
        public async Task<object> GetCategories()
        {
            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

            var result = await categoryService.GetFilteredAndPaginatedAsync(
                page: 1,
                pageSize: 100,
                keyword: null,
                status: "active"
            );

            return new
            {
                total = result.TotalItems,
                categories = result.Items.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description
                })
            };
        }
    }
}
