using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using StoreApp.Shared;

namespace StoreApp.Client.Services;

public interface IStatisticsService
{
    Task<OverviewStatsDTO?> GetOverviewStats();
    Task<List<RevenueDataPoint>> GetRevenueByPeriod(int days = 7);
    Task<List<ProductSalesDTO>> GetBestSellers(int limit = 10, int days = 7);
    Task<List<ProductInventoryDTO>> GetLowStockProducts(int threshold = 10);
    Task<OrderStatsDTO?> GetOrderStats(int days = 7);
}

public class StatisticsService : IStatisticsService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public StatisticsService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<OverviewStatsDTO?> GetOverviewStats()
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _http.GetFromJsonAsync<OverviewStatsDTO>("api/statistics/overview");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<RevenueDataPoint>> GetRevenueByPeriod(int days = 7)
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<RevenueDataPoint>>($"api/statistics/revenue?days={days}");
            return result ?? new List<RevenueDataPoint>();
        }
        catch
        {
            return new List<RevenueDataPoint>();
        }
    }

    public async Task<List<ProductSalesDTO>> GetBestSellers(int limit = 10, int days = 7)
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<ProductSalesDTO>>($"api/statistics/bestsellers?limit={limit}&days={days}");
            return result ?? new List<ProductSalesDTO>();
        }
        catch
        {
            return new List<ProductSalesDTO>();
        }
    }

    public async Task<List<ProductInventoryDTO>> GetLowStockProducts(int threshold = 10)
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<ProductInventoryDTO>>($"api/statistics/lowstock?threshold={threshold}");
            return result ?? new List<ProductInventoryDTO>();
        }
        catch
        {
            return new List<ProductInventoryDTO>();
        }
    }

    public async Task<OrderStatsDTO?> GetOrderStats(int days = 7)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _http.GetFromJsonAsync<OrderStatsDTO>($"api/statistics/orders?days={days}");
        }
        catch
        {
            return null;
        }
    }
}


