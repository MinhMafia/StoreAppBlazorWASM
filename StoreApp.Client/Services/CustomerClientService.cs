// Services/CustomerClientService.cs
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;

public interface ICustomerClientService
{
    Task<ResultPaginatedDTO<CustomerResponseDTO>> GetCustomers(int page, int pageSize, string? keyword, string? status);
    Task<CustomerResponseDTO?> GetCustomerById(int id);
    Task<CustomerResponseDTO> CreateCustomer(CustomerCreateDTO dto);
    Task<CustomerResponseDTO> UpdateCustomer(int id, CustomerUpdateDTO dto);
    Task<CustomerResponseDTO> UpdateCustomerStatus(int id, bool isActive);
}

public class CustomerClientService : ICustomerClientService
{
    private readonly HttpClient _http;

    public CustomerClientService(HttpClient http)
    {
        _http = http;
    }

    // GET - Lấy danh sách có phân trang và filter
    public async Task<ResultPaginatedDTO<CustomerResponseDTO>> GetCustomers(
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

        var url = QueryHelpers.AddQueryString("api/customers", queryParams);
        return await _http.GetFromJsonAsync<ResultPaginatedDTO<CustomerResponseDTO>>(url) 
               ?? new ResultPaginatedDTO<CustomerResponseDTO>();
    }

    // GET - Lấy chi tiết
    public async Task<CustomerResponseDTO?> GetCustomerById(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<CustomerResponseDTO>($"api/customers/{id}");
        }
        catch
        {
            return null;
        }
    }

    // POST - Tạo mới
    public async Task<CustomerResponseDTO> CreateCustomer(CustomerCreateDTO dto)
    {
        var response = await _http.PostAsJsonAsync("api/customers", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerResponseDTO>() 
               ?? throw new Exception("Failed to create customer");
    }

    // PATCH - Cập nhật thông tin
    public async Task<CustomerResponseDTO> UpdateCustomer(int id, CustomerUpdateDTO dto)
    {
        var response = await _http.PatchAsJsonAsync($"api/customers/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerResponseDTO>() 
               ?? throw new Exception("Failed to update customer");
    }

    // PUT - Cập nhật trạng thái active
    public async Task<CustomerResponseDTO> UpdateCustomerStatus(int id, bool isActive)
    {
        var dto = new CustomerUpdateActiveDTO { IsActive = isActive };
        var response = await _http.PutAsJsonAsync($"api/customers/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerResponseDTO>() 
               ?? throw new Exception("Failed to update customer status");
    }
}

