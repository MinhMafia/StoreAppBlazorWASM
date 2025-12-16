using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using StoreApp.Shared.DTO;

namespace StoreApp.Client.Services;

public interface IReportsService
{
    Task<SalesSummaryDTO?> GetSalesSummary(DateTime? fromDate, DateTime? toDate);
    Task<List<RevenueByDayDTO>> GetRevenueByDay(DateTime? fromDate, DateTime? toDate);
    Task<List<HighValueInventoryDTO>> GetHighValueInventory(int limit = 100);
    Task<PeriodComparisonDTO?> GetPeriodComparison(DateTime? fromDate, DateTime? toDate);
    Task<List<TopProductReportDTO>> GetTopProducts(DateTime? fromDate, DateTime? toDate, int limit = 10);
    Task<List<TopCustomerReportDTO>> GetTopCustomers(DateTime? fromDate, DateTime? toDate, int limit = 10);
    Task<List<SalesByStaffDTO>> GetSalesByStaff(DateTime? fromDate, DateTime? toDate);
    Task ExportSalesReport(DateTime? fromDate, DateTime? toDate, string format);
    Task ExportInventoryReport(string format);
}

public class ReportsService : IReportsService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;

    public ReportsService(HttpClient http, ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _http = http;
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
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

    public async Task<SalesSummaryDTO?> GetSalesSummary(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            return await _http.GetFromJsonAsync<SalesSummaryDTO>($"api/reports/summary{queryParams}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<RevenueByDayDTO>> GetRevenueByDay(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            var result = await _http.GetFromJsonAsync<List<RevenueByDayDTO>>($"api/reports/revenue-by-day{queryParams}");
            return result ?? new List<RevenueByDayDTO>();
        }
        catch
        {
            return new List<RevenueByDayDTO>();
        }
    }

    public async Task<List<HighValueInventoryDTO>> GetHighValueInventory(int limit = 100)
    {
        try
        {
            await SetAuthHeaderAsync();
            var result = await _http.GetFromJsonAsync<List<HighValueInventoryDTO>>($"api/reports/high-value-inventory?limit={limit}");
            return result ?? new List<HighValueInventoryDTO>();
        }
        catch
        {
            return new List<HighValueInventoryDTO>();
        }
    }

    public async Task<PeriodComparisonDTO?> GetPeriodComparison(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            return await _http.GetFromJsonAsync<PeriodComparisonDTO>($"api/reports/period-comparison{queryParams}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TopProductReportDTO>> GetTopProducts(DateTime? fromDate, DateTime? toDate, int limit = 10)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            var separator = queryParams.Contains("?") ? "&" : "?";
            var result = await _http.GetFromJsonAsync<List<TopProductReportDTO>>($"api/reports/top-products{queryParams}{separator}limit={limit}");
            return result ?? new List<TopProductReportDTO>();
        }
        catch
        {
            return new List<TopProductReportDTO>();
        }
    }

    public async Task<List<TopCustomerReportDTO>> GetTopCustomers(DateTime? fromDate, DateTime? toDate, int limit = 10)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            var separator = queryParams.Contains("?") ? "&" : "?";
            var result = await _http.GetFromJsonAsync<List<TopCustomerReportDTO>>($"api/reports/top-customers{queryParams}{separator}limit={limit}");
            return result ?? new List<TopCustomerReportDTO>();
        }
        catch
        {
            return new List<TopCustomerReportDTO>();
        }
    }

    public async Task<List<SalesByStaffDTO>> GetSalesByStaff(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            var result = await _http.GetFromJsonAsync<List<SalesByStaffDTO>>($"api/reports/sales-by-staff{queryParams}");
            return result ?? new List<SalesByStaffDTO>();
        }
        catch
        {
            return new List<SalesByStaffDTO>();
        }
    }

    public async Task ExportSalesReport(DateTime? fromDate, DateTime? toDate, string format)
    {
        try
        {
            await SetAuthHeaderAsync();
            var queryParams = BuildDateQueryParams(fromDate, toDate);
            var separator = queryParams.Contains("?") ? "&" : "?";
            var response = await _http.GetAsync($"api/reports/export-sales{queryParams}{separator}format={format}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = format.ToLower() == "xlsx" 
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv";
                var extension = format.ToLower() == "xlsx" ? "xlsx" : "csv";
                var fileName = $"BaoCaoBanHang_{DateTime.Now:yyyyMMddHHmmss}.{extension}";
                
                await DownloadFile(content, fileName, contentType);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xuất báo cáo: {ex.Message}");
        }
    }

    public async Task ExportInventoryReport(string format)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.GetAsync($"api/reports/export-inventory?format={format}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = format.ToLower() == "xlsx" 
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv";
                var extension = format.ToLower() == "xlsx" ? "xlsx" : "csv";
                var fileName = $"BaoCaoTonKho_{DateTime.Now:yyyyMMddHHmmss}.{extension}";
                
                await DownloadFile(content, fileName, contentType);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xuất báo cáo: {ex.Message}");
        }
    }

    private string BuildDateQueryParams(DateTime? fromDate, DateTime? toDate)
    {
        var queryParams = new List<string>();
        if (fromDate.HasValue)
        {
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        }
        if (toDate.HasValue)
        {
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        }
        return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
    }

    private async Task DownloadFile(byte[] content, string fileName, string contentType)
    {
        // Sử dụng JavaScript để tải file
        var base64 = Convert.ToBase64String(content);
        await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, base64, contentType);
    }
}

