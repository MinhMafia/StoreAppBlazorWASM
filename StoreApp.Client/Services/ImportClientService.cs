using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;

namespace StoreApp.Client.Services;

public interface IImportClientService
{
    Task<PaginationResult<ImportListItemDTO>> GetImportsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? sortBy);

    Task<ImportDetailDTO?> GetImportDetailAsync(int id);

    Task<bool> CreateImportAsync(CreateImportDTO dto);
}

public class ImportClientService : IImportClientService
{
    private readonly HttpClient _http;

    public ImportClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaginationResult<ImportListItemDTO>> GetImportsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? sortBy)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(search))
            query["search"] = search;
        if (!string.IsNullOrWhiteSpace(status))
            query["status"] = status;
        if (!string.IsNullOrWhiteSpace(sortBy))
            query["sortBy"] = sortBy;

        var url = QueryHelpers.AddQueryString("api/imports", query);

        return await _http.GetFromJsonAsync<PaginationResult<ImportListItemDTO>>(url)
               ?? new PaginationResult<ImportListItemDTO>();
    }

    public async Task<ImportDetailDTO?> GetImportDetailAsync(int id)
    {
        return await _http.GetFromJsonAsync<ImportDetailDTO>($"api/imports/{id}");
    }

    public async Task<bool> CreateImportAsync(CreateImportDTO dto)
    {
        var response = await _http.PostAsJsonAsync("api/imports", dto);
        return response.IsSuccessStatusCode;
    }
}
