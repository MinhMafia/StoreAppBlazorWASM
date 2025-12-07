using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared; 

public interface IOrdersClientService
{
    Task<OrderDTO> CreateTemporaryOrderAsync();                           
    Task<bool> SaveFinalOrderAsync(OrderDTO orderDto);                            
    Task<ResultPaginatedDTO<OrderDTO>> LoadOrdersAdvanced(
        int pageNumber, int pageSize,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null);  
    Task<List<OrderItemReponse>> GetOrderItemsByOrderIdAsync(int orderId); 
        
                                                  
}

public class OrdersClientService : IOrdersClientService
{
    private readonly HttpClient _http;

    public OrdersClientService(HttpClient http) 
    {
        _http = http;
    }


    // 1. Tạo đơn tạm (draft) → trả về OrderDTO có Id + OrderNumber
    public async Task<OrderDTO> CreateTemporaryOrderAsync()
    {
        var response = await _http.PostAsync("api/orders/create-temp", null);

        if (!response.IsSuccessStatusCode)
            return new OrderDTO();

        var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return order ?? new OrderDTO();
    }

    // 2. LƯU ĐƠN CHÍNH THỨC (mới thêm) → gửi toàn bộ OrderDTO đã chỉnh sửa lên
    public async Task<bool> SaveFinalOrderAsync(OrderDTO orderDto)
    {
        if (orderDto == null) return false;

        var response = await _http.PostAsJsonAsync("api/orders/create", orderDto);

        if (!response.IsSuccessStatusCode)
            return false;

        // API trả về true/false dạng JSON hoặc plain text
        var result = await response.Content.ReadFromJsonAsync<bool>();
        return result;
    }

    // 3. Tìm kiếm + phân trang (đã có, chỉ tối ưu lại chút)
    public async Task<ResultPaginatedDTO<OrderDTO>> LoadOrdersAdvanced(
        int pageNumber,
        int pageSize,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["pageNumber"] = pageNumber.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["status"] = status,
            ["startDate"] = startDate?.ToString("o"),
            ["endDate"] = endDate?.ToString("o"),
            ["search"] = search
        }
        .Where(x => x.Value != null)
        .ToDictionary(x => x.Key, x => x.Value!);

        var url = QueryHelpers.AddQueryString("api/orders/search", queryParams);

        try
        {
            var result = await _http.GetFromJsonAsync<ResultPaginatedDTO<OrderDTO>>(url);
            return result ?? new ResultPaginatedDTO<OrderDTO>();
        }
        catch
        {
            return new ResultPaginatedDTO<OrderDTO>();
        }
    }

    // Lấy danh sách các OrderItemReponse theo OrderId
    public async Task<List<OrderItemReponse>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _http.GetFromJsonAsync<List<OrderItemReponse>>(
            $"api/orderitem/byorder/{orderId}"
        ) ?? new List<OrderItemReponse>();
    }

}

// Kết quả phân trang chung


