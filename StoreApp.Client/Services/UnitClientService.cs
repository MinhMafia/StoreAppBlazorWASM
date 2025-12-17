using System.Net.Http.Json;
using StoreApp.Shared;

public interface IUnitClientService
{
    Task<List<UnitDTO>> GetAllUnits();
}

public class UnitClientService : IUnitClientService
{
    private readonly HttpClient _http;

    public UnitClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<UnitDTO>> GetAllUnits()
    {
        return await _http.GetFromJsonAsync<List<UnitDTO>>("api/units")
               ?? new List<UnitDTO>();
    }
}