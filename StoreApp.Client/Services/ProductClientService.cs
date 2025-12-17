using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;
using Microsoft.AspNetCore.Components.Forms;

public interface IProductClientService
{
    Task<PaginationResult<ProductDTO>> GetProducts(int page, int pageSize, string? search, decimal? min, decimal? max, string? sortBy = null, int? categoryId = null, int? supplierId = null, int? status = null);
    Task<bool> DeleteProduct(int id);
    Task<ProductDTO> GetProductById(int id);
    Task<ProductDTO?> CreateProduct(ProductDTO product);
    Task<ProductDTO?> UpdateProduct(int id, ProductDTO product);
    Task<string?> UploadProductImage(int productId, IBrowserFile file); // ← THÊM
}

public class ProductClientService : IProductClientService
{
    private readonly HttpClient _http;

    public ProductClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaginationResult<ProductDTO>> GetProducts(int page, int pageSize, string? search, decimal? min, decimal? max, string? sortBy = null, int? categoryId = null, int? supplierId = null, int? status = null)
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
        if (supplierId.HasValue) queryParams["supplierId"] = supplierId.ToString();
        if (status.HasValue) queryParams["status"] = status.ToString();

        var url = QueryHelpers.AddQueryString("api/products/paginated", queryParams);

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
        return await _http.GetFromJsonAsync<ProductDTO>($"api/products/{id}")
               ?? new ProductDTO();
    }

    public async Task<ProductDTO?> CreateProduct(ProductDTO product)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/products", product);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDTO>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductDTO?> UpdateProduct(int id, ProductDTO product)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/products/{id}", product);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDTO>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ← THÊM METHOD UPLOAD ẢNH
    public async Task<string?> UploadProductImage(int productId, IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(5 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "image", file.Name);

            var response = await _http.PostAsync($"api/products/upload-image?productId={productId}", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>();
                return result?.ImageUrl;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private class UploadImageResponse
    {
        public string? ImageUrl { get; set; }
    }
}