using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared; // Nhớ using DTO

public interface IProductClientService
{
    Task<PaginationResult<ProductDTO>> GetProducts(int page, int pageSize, string? search, decimal? min, decimal? max, string? sortBy = null, int? categoryId = null, int? status = null);
    Task<bool> DeleteProduct(int id);
    Task<ProductDTO> GetProductById(int id);
}

public class ProductClientService : IProductClientService
{
    private readonly HttpClient _http;

    public ProductClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaginationResult<ProductDTO>> GetProducts(int page, int pageSize, string? search, decimal? min, decimal? max, string? sortBy = null, int? categoryId = null, int? status = null)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrEmpty(search)) queryParams["search"] = search;
        if (min.HasValue) queryParams["minPrice"] = min.ToString();
        if (max.HasValue) queryParams["maxPrice"] = max.ToString();
        if (!string.IsNullOrEmpty(sortBy)) queryParams["sortBy"] = sortBy;
        if (categoryId.HasValue) queryParams["categoryId"] = categoryId.ToString();
        if (status.HasValue) queryParams["status"] = status.ToString();

        var url = QueryHelpers.AddQueryString("api/products/paginated", queryParams);

        // Gọi API; nếu null trả về kết quả rỗng để tránh null reference
        return await _http.GetFromJsonAsync<PaginationResult<ProductDTO>>(url)
               ?? new PaginationResult<ProductDTO>();
    }

    public async Task<bool> DeleteProduct(int id)
    {
        var response = await _http.DeleteAsync($"api/products/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<ProductDTO> GetProductById(int id)
    {
        // Gọi API Backend: GET api/products/{id}
        return await _http.GetFromJsonAsync<ProductDTO>($"api/products/{id}")
               ?? new ProductDTO();
    }
}