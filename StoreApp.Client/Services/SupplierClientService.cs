using System.Net.Http.Json;
using StoreApp.Shared;

public interface ISupplierClientService
{
    Task<List<SupplierDTO>> GetAllSuppliers();
    Task<List<SupplierDTO>> GetSuppliersAsync();
}

public class SupplierClientService : ISupplierClientService
{
    private readonly HttpClient _http;

    public SupplierClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<SupplierDTO>> GetAllSuppliers()
    {
        return await _http.GetFromJsonAsync<List<SupplierDTO>>("api/suppliers")
               ?? new List<SupplierDTO>();
    }

    public async Task<List<SupplierDTO>> GetSuppliersAsync()
    {
        return await GetAllSuppliers();
    }
}