using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class CustomerProductPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomerProductPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Tìm kiếm sản phẩm trong cửa hàng")]
        public async Task<object> SearchProducts(
            [Description("Từ khóa tìm kiếm (tên sản phẩm)")] string? keyword = null,
            [Description("ID danh mục")] int? categoryId = null,
            [Description("Giá tối thiểu")] decimal? minPrice = null,
            [Description("Giá tối đa")] decimal? maxPrice = null,
            [Description("Số trang")] int page = 1,
            [Description("Số kết quả/trang")] int limit = 12,
            [Description("Sắp xếp: price_asc, price_desc, name_asc, name_desc")] string? sortBy = null)
        {
            limit = Math.Min(limit, 20);

            using var scope = _serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

            var result = await productService.GetPaginatedProductsAsync(
                page: page,
                pageSize: limit,
                search: keyword,
                categoryId: categoryId,
                supplierId: null,
                minPrice: minPrice,
                maxPrice: maxPrice,
                sortBy: sortBy ?? "",
                status: 1
            );

            var inStockProducts = result.Items
                .Where(p => p.Inventory != null && p.Inventory.Quantity > 0)
                .ToList();

            return new
            {
                total = inStockProducts.Count,
                page,
                hasMore = page * limit < result.TotalItems,
                products = inStockProducts.Select(p => new
                {
                    p.Id,
                    Name = p.ProductName,
                    p.Price,
                    CategoryName = p.Category?.Name,
                    InStock = (p.Inventory?.Quantity ?? 0) > 0,
                    ImageUrl = p.ImageUrl
                })
            };
        }

        [KernelFunction, Description("Xem chi tiết một sản phẩm")]
        public async Task<object> GetProductDetail(
            [Description("ID sản phẩm")] int productId)
        {
            using var scope = _serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

            var product = await productService.GetProductByIdAsync(productId);
            if (product == null)
                return new { error = "Không tìm thấy sản phẩm" };

            return new
            {
                product.Id,
                Name = product.ProductName,
                product.Description,
                product.Price,
                CategoryName = product.Category?.Name,
                InStock = (product.Inventory?.Quantity ?? 0) > 0,
                Quantity = product.Inventory?.Quantity ?? 0,
                ImageUrl = product.ImageUrl
            };
        }
    }
}
