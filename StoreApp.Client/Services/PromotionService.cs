using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using StoreApp.Shared;

namespace StoreApp.Client.Services;

public class PromotionOverviewStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Scheduled { get; set; }
    public int Expired { get; set; }
    public int Inactive { get; set; }
}

public class PromotionDetailStats
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int TotalRedemptions { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public int UniqueCustomers { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class PromotionRedemption
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal OrderAmount { get; set; }
    public DateTime RedeemedAt { get; set; }
}

public interface IPromotionService
{
    Task<ResultPaginatedDTO<PromotionDTO>> GetPromotionsPaginated(int page = 1, int pageSize = 12, string? search = null, string? status = null, string? type = null);
    Task<PromotionDTO?> GetPromotionById(int id);
    Task<PromotionDTO?> CreatePromotion(PromotionDTO promotion);
    Task<bool> UpdatePromotion(int id, PromotionDTO promotion);
    Task<bool> DeletePromotion(int id);
    Task<bool> TogglePromotionActive(int id);
    Task<PromotionOverviewStats?> GetPromotionOverviewStats();
    Task<PromotionDetailStats?> GetPromotionStats(int id);
    Task<List<PromotionRedemption>> GetPromotionRedemptions(int id);
}

public class PromotionService : IPromotionService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public PromotionService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            return token?.Trim('"');
        }
        catch
        {
            return null;
        }
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await GetAuthTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<ResultPaginatedDTO<PromotionDTO>> GetPromotionsPaginated(int page = 1, int pageSize = 12, string? search = null, string? status = null, string? type = null)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            };

            if (!string.IsNullOrEmpty(search)) queryParams["search"] = search;
            if (!string.IsNullOrEmpty(status) && status != "all") queryParams["status"] = status;
            if (!string.IsNullOrEmpty(type) && type != "all") queryParams["type"] = type;

            var url = QueryHelpers.AddQueryString("api/promotions/paginated", queryParams);
            var result = await _http.GetFromJsonAsync<ResultPaginatedDTO<PromotionDTO>>(url);
            return result ?? new ResultPaginatedDTO<PromotionDTO>();
        }
        catch
        {
            return new ResultPaginatedDTO<PromotionDTO>();
        }
    }

    public async Task<PromotionDTO?> GetPromotionById(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _http.GetFromJsonAsync<PromotionDTO>($"api/promotions/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<PromotionDTO?> CreatePromotion(PromotionDTO promotion)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync("api/promotions", promotion);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PromotionDTO>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdatePromotion(int id, PromotionDTO promotion)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.PutAsJsonAsync($"api/promotions/{id}", promotion);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeletePromotion(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.DeleteAsync($"api/promotions/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TogglePromotionActive(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.PatchAsync($"api/promotions/{id}/toggle", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PromotionOverviewStats?> GetPromotionOverviewStats()
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _http.GetFromJsonAsync<PromotionOverviewStats>("api/promotions/overview-stats");
        }
        catch
        {
            return null;
        }
    }

    public async Task<PromotionDetailStats?> GetPromotionStats(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _http.GetFromJsonAsync<PromotionDetailStats>($"api/promotions/{id}/stats");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PromotionRedemption>> GetPromotionRedemptions(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<PromotionRedemption>>($"api/promotions/{id}/redemptions");
            return result ?? new List<PromotionRedemption>();
        }
        catch
        {
            return new List<PromotionRedemption>();
        }
    }
}

