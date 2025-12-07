using System.ComponentModel;
using Microsoft.SemanticKernel;
using StoreApp.Services;

namespace StoreApp.Services.AI.Plugins
{
    public class ProductPlugin
    {
        private readonly IServiceProvider _serviceProvider;

        public ProductPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction, Description("Tìm kiếm và lọc sản phẩm theo nhiều tiêu chí. Hỗ trợ pagination.")]
        public async Task<object> QueryProducts(
            [Description("Từ khóa tìm theo tên sản phẩm")] string? keyword = null,
            [Description("Lọc theo ID danh mục")] int? categoryId = null,
            [Description("Lọc theo ID nhà cung cấp")] int? supplierId = null,
            [Description("Giá tối thiểu")] decimal? minPrice = null,
            [Description("Giá tối đa")] decimal? maxPrice = null,
            [Description("true=còn hàng, false=hết hàng")] bool? inStock = null,
            [Description("Trạng thái hoạt động")] bool? isActive = null,
            [Description("Số trang (mặc định 1)")] int page = 1,
            [Description("Số kết quả/trang (mặc định 20, tối đa 50)")] int limit = 20,
            [Description("Sắp xếp: price_asc, price_desc, name_asc, name_desc")] string? sortBy = null)
        {
            limit = Math.Min(limit, 50);

            using var scope = _serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

            int? status = isActive.HasValue ? (isActive.Value ? 1 : 0) : null;

            var result = await productService.GetPaginatedProductsAsync(
                page: page,
                pageSize: limit,
                search: keyword,
                categoryId: categoryId,
                supplierId: supplierId,
                minPrice: minPrice,
                maxPrice: maxPrice,
                sortBy: sortBy ?? "",
                status: status
            );

            var items = result.Items;
            if (inStock.HasValue)
            {
                items = inStock.Value
                    ? items.Where(p => p.Inventory != null && p.Inventory.Quantity > 0).ToList()
                    : items.Where(p => p.Inventory == null || p.Inventory.Quantity <= 0).ToList();
            }

            return new
            {
                total = result.TotalItems,
                page,
                pageSize = limit,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                hasMore = page * limit < result.TotalItems,
                products = items.Select(p => new
                {
                    p.Id,
                    Name = p.ProductName,
                    p.Sku,
                    p.Price,
                    CategoryName = p.Category?.Name,
                    SupplierName = p.Supplier?.Name,
                    Quantity = p.Inventory?.Quantity ?? 0,
                    Status = p.IsActive ? "active" : "inactive"
                })
            };
        }
    }
}
