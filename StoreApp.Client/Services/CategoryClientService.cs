// Services/CategoryClientService.cs
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;

public interface ICategoryClientService
{
    Task<ResultPaginatedDTO<CategoryResponseDTO>> GetCategories(int page, int pageSize, string? keyword, string? status);
    Task<CategoryResponseDTO?> GetCategoryById(int id);
    Task<CategoryResponseDTO> CreateCategory(CategoryCreateDTO dto);
    Task<CategoryResponseDTO> UpdateCategory(int id, CategoryUpdateDTO dto);
    Task<CategoryResponseDTO> UpdateCategoryStatus(int id, bool isActive);
}

public class CategoryClientService : ICategoryClientService
{
    private readonly HttpClient _http;

    public CategoryClientService(HttpClient http)
    {
        _http = http;
    }

    // GET - Lấy danh sách có phân trang và filter
    public async Task<ResultPaginatedDTO<CategoryResponseDTO>> GetCategories(
        int page, 
        int pageSize, 
        string? keyword, 
        string? status)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrEmpty(keyword))
            queryParams["keyword"] = keyword;
        
        if (!string.IsNullOrEmpty(status) && status != "all")
            queryParams["status"] = status;

        var url = QueryHelpers.AddQueryString("api/categories", queryParams);
        return await _http.GetFromJsonAsync<ResultPaginatedDTO<CategoryResponseDTO>>(url) 
               ?? new ResultPaginatedDTO<CategoryResponseDTO>();
    }

    // GET - Lấy chi tiết
    public async Task<CategoryResponseDTO?> GetCategoryById(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CategoryResponseDTO>($"api/categories/{id}");
        }
        catch
        {
            return null;
        }
    }

    // POST - Tạo mới
    public async Task<CategoryResponseDTO> CreateCategory(CategoryCreateDTO dto)
    {
        var response = await _http.PostAsJsonAsync("api/categories", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryResponseDTO>() 
               ?? throw new Exception("Failed to create category");
    }

    // PATCH - Cập nhật thông tin
    public async Task<CategoryResponseDTO> UpdateCategory(int id, CategoryUpdateDTO dto)
    {
        var response = await _http.PatchAsJsonAsync($"api/categories/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryResponseDTO>() 
               ?? throw new Exception("Failed to update category");
    }

    // PUT - Cập nhật trạng thái active
    public async Task<CategoryResponseDTO> UpdateCategoryStatus(int id, bool isActive)
    {
        var dto = new CategoryUpdateActiveDTO { IsActive = isActive };
        var response = await _http.PutAsJsonAsync($"api/categories/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryResponseDTO>() 
               ?? throw new Exception("Failed to update category status");
    }
}