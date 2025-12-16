using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using StoreApp.Shared;
using Microsoft.AspNetCore.WebUtilities;

namespace StoreApp.Client.Services;

public interface IAuditClientService
{
    Task<List<UserDTO>> GetAllUsersAsync();
    Task<ResultPaginatedDTO<ActivityLogCreateDTO>> GetFilteredLogsAsync(
        int page = 1,
        int size = 10,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
    
}

public class AuditClientService : IAuditClientService
{
    private readonly HttpClient _http;
 

    public AuditClientService(HttpClient http)
    {
        _http = http;
      
    }

    // Lấy danh sách user
    public async Task<List<UserDTO>> GetAllUsersAsync()
    {
        var response = await _http.GetAsync("api/users/getalluser");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Call API failed: {response.StatusCode}");
        }

        var users = await response.Content.ReadFromJsonAsync<List<UserDTO>>();
        return users ?? new List<UserDTO>();
    }

        public async Task<ResultPaginatedDTO<ActivityLogCreateDTO>> GetFilteredLogsAsync(
        int page = 1,
        int size = 10,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["size"] = size.ToString(),
            ["userId"] = userId?.ToString(),
            ["startDate"] = startDate?.ToString("yyyy-MM-dd"),
            ["endDate"] = endDate?.ToString("yyyy-MM-dd")
        };

        var url = QueryHelpers.AddQueryString(
            "api/activitylog/filter",
            queryParams!);

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDTO>();
            throw new ApplicationException(error?.Message ?? "Call API failed");
        }

        return await response.Content
            .ReadFromJsonAsync<ResultPaginatedDTO<ActivityLogCreateDTO>>()
            ?? new ResultPaginatedDTO<ActivityLogCreateDTO>();
    }


}
public class ApiErrorDTO
{
    public string Message { get; set; } = string.Empty;
}


