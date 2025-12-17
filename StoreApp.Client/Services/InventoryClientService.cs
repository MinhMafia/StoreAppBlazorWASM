using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;

namespace StoreApp.Client.Services;

public interface IInventoryClientService
{
    Task<PaginationResult<InventoryListItemDTO>> GetInventoryAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? stockStatus);

    Task<InventoryStatsDTO?> GetStatsAsync();

    Task<bool> AdjustInventoryAsync(int inventoryId, int newQuantity, string? reason, decimal? newCost = null, int? productId = null);
    Task<InventoryAuditResultDTO?> AuditInventoryAsync(bool autoDeactivate = false);
}

public class InventoryClientService : IInventoryClientService
{
    private readonly HttpClient _http;

    public InventoryClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaginationResult<InventoryListItemDTO>> GetInventoryAsync(
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? stockStatus)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(search))
            query["search"] = search;
        if (!string.IsNullOrWhiteSpace(sortBy))
            query["sortBy"] = sortBy;
        if (!string.IsNullOrWhiteSpace(stockStatus))
            query["stockStatus"] = stockStatus;

        var url = QueryHelpers.AddQueryString("api/inventory/paginated", query);

        return await _http.GetFromJsonAsync<PaginationResult<InventoryListItemDTO>>(url)
               ?? new PaginationResult<InventoryListItemDTO>();
    }

    public async Task<InventoryStatsDTO?> GetStatsAsync()
    {
        return await _http.GetFromJsonAsync<InventoryStatsDTO>("api/inventory/stats");
    }

    public async Task<bool> AdjustInventoryAsync(
        int inventoryId,
        int newQuantity,
        string? reason,
        decimal? newCost = null,
        int? productId = null)
    {
        var payload = new AdjustInventoryRequestDTO
        {
            InventoryId = inventoryId,
            NewQuantity = newQuantity,
            Reason = reason,
            NewCost = newCost,
            ProductId = productId ?? 0
        };

        var response = await _http.PostAsJsonAsync("api/inventory/adjust", payload);
        return response.IsSuccessStatusCode;
    }

    public async Task<InventoryAuditResultDTO?> AuditInventoryAsync(bool autoDeactivate = false)
    {
        var url = $"api/inventory/audit?autoDeactivate={autoDeactivate}";
        return await _http.GetFromJsonAsync<InventoryAuditResultDTO>(url);
    }
}