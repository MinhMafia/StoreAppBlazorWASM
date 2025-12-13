using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using StoreApp.Shared;
using Microsoft.JSInterop;

public interface IOrdersClientService
{
    Task<OrderDTO> CreateTemporaryOrderAsync();
    Task<OrderDTO?> SaveFinalOrderAsync(OrderDTO orderDto);
    Task<ResultPaginatedDTO<OrderDTO>> LoadOrdersAdvanced(
        int pageNumber, int pageSize,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null);
    Task<List<OrderItemReponse>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<PromotionDTO?> GetPromotionByIdAsync(int orderId);
    Task<PaginationResult<ProductDTO>> GetProductsPagedAndSearchedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null);

    Task<ResultPaginatedDTO<CustomerDTO>> GetCustomersPagedAndSearchedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null
    );
    Task<bool> HandleProcessClick(OrderDTO order);
    Task<List<PromotionDTO>> GetListActivePromotion();
    Task<bool> ReduceMultipleAsync(List<ReduceInventoryDto> items);
    Task<bool> ApplyPromotionAsync(ApplyPromotionRequest req);
    Task<bool> SaveListOrderItem(List<OrderItemReponse> items);
    Task<PaymentResult> PayWithMomoAsync(int orderId, decimal amount);
    Task<PaymentResult> PayOfflineAsync(int orderId, decimal amount);
    Task<bool> HandleCancelClick(int orderId);
    Task<OrderDTO> CreateTemporaryOnlineOrderAsync();
    Task<PaymentResult> PayCashInOnlineOrderAsync(int orderId, decimal amount);
    Task<PaymentResult> PayWithMomoWitOnlineOrderAsync(int orderId, decimal amount);
    Task<OrderDTO?> GetOrderDTOAsync(int orderId);
    Task<List<OrderDTO>> GetMyOrdersAsync();







}

public class OrdersClientService : IOrdersClientService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public OrdersClientService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }
    public async Task<OrderDTO> CreateTemporaryOnlineOrderAsync()
    {
        var response = await _http.PostAsync("api/orders/createonlineordertemp", null);

        if (!response.IsSuccessStatusCode)
            return new OrderDTO();

        var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return order ?? new OrderDTO();
    }



    public async Task<OrderDTO> CreateTemporaryOrderAsync()
    {
        var response = await _http.PostAsync("api/orders/create-temp", null);

        if (!response.IsSuccessStatusCode)
            return new OrderDTO();

        var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return order ?? new OrderDTO();
    }

    public async Task<OrderDTO?> SaveFinalOrderAsync(OrderDTO orderDto)
    {
        if (orderDto == null) return null;

        var response = await _http.PostAsJsonAsync("api/orders/create", orderDto);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return result;
    }

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

    public async Task<List<OrderItemReponse>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _http.GetFromJsonAsync<List<OrderItemReponse>>(
            $"api/orderitem/byorder/{orderId}"
        ) ?? new List<OrderItemReponse>();
    }

    public async Task<PromotionDTO?> GetPromotionByIdAsync(int Id)
    {
        try
        {
            return await _http.GetFromJsonAsync<PromotionDTO?>(
                $"api/promotions/{Id}"
            );
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }


    public async Task<PaginationResult<ProductDTO>> GetProductsPagedAndSearchedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null)
    {
        // Validate
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        searchKeyword = searchKeyword?.Trim();

        string url;
        if (string.IsNullOrEmpty(searchKeyword))
        {
            url = $"api/products/available?page={pageNumber}&pageSize={pageSize}";
        }
        else
        {
            var queryParams = new Dictionary<string, string>
            {
                ["page"] = pageNumber.ToString(),
                ["pageSize"] = pageSize.ToString(),
                ["keyword"] = searchKeyword
            };

            url = QueryHelpers.AddQueryString("api/products/search", queryParams);
        }

        try
        {
            var result = await _http.GetFromJsonAsync<PaginationResult<ProductDTO>>(url);
            return result ?? new PaginationResult<ProductDTO>();
        }
        catch
        {
            return new PaginationResult<ProductDTO>();
        }
    }

    public async Task<ResultPaginatedDTO<CustomerDTO>> GetCustomersPagedAndSearchedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword
    )
    {
        // Build query string
        var queryParams = new Dictionary<string, string>
        {
            ["page"] = pageNumber.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(searchKeyword))
            queryParams["search"] = searchKeyword;

        string url = QueryHelpers.AddQueryString("api/customers/paginated", queryParams);

        try
        {
            var result = await _http.GetFromJsonAsync<ResultPaginatedDTO<CustomerDTO>>(url);
            return result ?? new ResultPaginatedDTO<CustomerDTO>();
        }
        catch
        {
            return new ResultPaginatedDTO<CustomerDTO>();
        }
    }

    public async Task<List<PromotionDTO>> GetListActivePromotion()
    {
        try
        {
            var data = await _http.GetFromJsonAsync<List<PromotionDTO>>("api/promotions/active");
            return data ?? new List<PromotionDTO>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading promotions: {ex.Message}");
            return new List<PromotionDTO>();
        }
    }

    public async Task<bool> SaveListOrderItem(List<OrderItemReponse> items)
    {
        if (items.Count == 0) return false;

        var response = await _http.PostAsJsonAsync("api/orderitem/create", items);
        if (!response.IsSuccessStatusCode) return false;

        // API co the tra ve bool JSON hoac plain text
        var json = await response.Content.ReadFromJsonAsync<bool?>();
        if (json.HasValue) return json.Value;

        var text = await response.Content.ReadAsStringAsync();
        return bool.TryParse(text, out var parsed) && parsed;
    }

    // Giam so luong nhieu san pham
    public async Task<bool> ReduceMultipleAsync(List<ReduceInventoryDto> items)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/inventory/reduce-multiple", items);

            if (response == null || !response.IsSuccessStatusCode)
                return false;

            return true;
        }
        catch
        {

            return false;
        }
    }

    // Apply promotion
    public async Task<bool> ApplyPromotionAsync(ApplyPromotionRequest req)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/promotions/apply",
                req
            );

            return response?.IsSuccessStatusCode ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaymentResult> PayWithMomoAsync(int orderId, decimal amount)
    {
        try
        {

            var body = new MomoPaymentRequestDTO
            {
                OrderId = orderId,
                Amount = amount,
                ReturnUrl = "",
                NotifyUrl = "https://stainful-asher-unfeigningly.ngrok-free.dev/api/payment/momo/ipn"
            };

            var res = await _http.PostAsJsonAsync("api/payment/momo/create", body);

            if (!res.IsSuccessStatusCode)
                return new PaymentResult { Success = false, Message = "Không tạo được payment MoMo" };

            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json == null || !json.ContainsKey("payUrl"))
                return new PaymentResult { Success = false, Message = "Thiếu payUrl" };
            string payUrl = json["payUrl"].ToString()!;


            await _js.InvokeVoidAsync("openMoMoPayment", payUrl);

            int counter = 0;

            while (true)
            {
                await Task.Delay(2000);
                counter++;

                var statusRes = await _http.GetAsync($"api/payment/status/{orderId}");
                if (statusRes.IsSuccessStatusCode)
                {
                    var data = await statusRes.Content.ReadFromJsonAsync<Dictionary<string, string>>();

                    if (data != null && data.ContainsKey("status"))
                    {
                        string status = data["status"];

                        if (status == "completed")
                        {
                            return new PaymentResult
                            {
                                Success = true,
                                Message = "Thanh toán thành công!"
                            };
                        }
                    }
                }

                if (counter >= 60)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Quá thời gian chờ thanh toán"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lỗi client: " + ex.Message
            };
        }
    }


    public async Task<PaymentResult> PayWithMomoWitOnlineOrderAsync(int orderId, decimal amount)
    {
        try
        {

            var body = new MomoPaymentRequestDTO
            {
                OrderId = orderId,
                Amount = amount,
                ReturnUrl = "",
                NotifyUrl = "https://stainful-asher-unfeigningly.ngrok-free.dev/api/payment/momo/ipnonline"
            };

            // 2) Gá»i API create
            var res = await _http.PostAsJsonAsync("api/payment/momo/create", body);

            if (!res.IsSuccessStatusCode)
                return new PaymentResult { Success = false, Message = "Không tạo được payment MoMo" };

            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json == null || !json.ContainsKey("payUrl"))
                return new PaymentResult { Success = false, Message = "Thiếu payUrl" };
            string payUrl = json["payUrl"].ToString()!;


            await _js.InvokeVoidAsync("openMoMoPayment", payUrl);

            int counter = 0;

            while (true)
            {
                await Task.Delay(2000);
                counter++;

                var statusRes = await _http.GetAsync($"api/payment/status/{orderId}");
                if (statusRes.IsSuccessStatusCode)
                {
                    var data = await statusRes.Content.ReadFromJsonAsync<Dictionary<string, string>>();

                    if (data != null && data.ContainsKey("status"))
                    {
                        string status = data["status"];

                        if (status == "completed")
                        {
                            return new PaymentResult
                            {
                                Success = true,
                                Message = "Thanh toán thành công!"
                            };
                        }
                    }
                }

                if (counter >= 60)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Quá thời gian chờ thanh toán"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lỗi client: " + ex.Message
            };
        }
    }

    public async Task<PaymentResult> PayOfflineAsync(int orderId, decimal amount)
    {
        try
        {
            var body = new
            {
                OrderId = orderId,
                Amount = amount,
                Method = "cash",
                Status = "completed"
            };

            var res = await _http.PostAsJsonAsync("api/payment/offlinepayment", body);

            if (!res.IsSuccessStatusCode)
                return new PaymentResult { Success = false, Message = "Không tạo được offline payment" };

            return new PaymentResult
            {
                Success = true,
                Message = "Thanh toán tiền mặt thành công!"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lỗi offline payment: " + ex.Message
            };
        }
    }

    public async Task<PaymentResult> PayCashInOnlineOrderAsync(int orderId, decimal amount)
    {
        try
        {
            var body = new
            {
                OrderId = orderId,
                Amount = amount,
                Method = "cash",
                Status = "pending"
            };

            var res = await _http.PostAsJsonAsync("api/payment/offlinepayment", body);

            if (!res.IsSuccessStatusCode)
                return new PaymentResult { Success = false, Message = "Không tạo được offline payment" };

            return new PaymentResult
            {
                Success = true,
                Message = "Thanh toán tiền mặt thành công!"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lỗi offline payment: " + ex.Message
            };
        }
    }

    //Xử lí đơn hàng
    public async Task<bool> HandleProcessClick(OrderDTO order)
    {
        var response = await _http.PostAsJsonAsync("/api/orders/process", order);

        if (response.IsSuccessStatusCode)
        {
            bool result = await response.Content.ReadFromJsonAsync<bool>();
            return result; // true / false
        }

        return false;
    }

    // Huy don
    public async Task<bool> HandleCancelClick(int orderId)
    {
        try
        {
            var response = await _http.PostAsync($"api/orders/{orderId}/cancel", null);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<bool?>();
            if (result.HasValue) return result.Value;

            var text = await response.Content.ReadAsStringAsync();
            return bool.TryParse(text, out var parsed) && parsed;
        }
        catch
        {
            return false;
        }
    }

    public async Task<OrderDTO?> GetOrderDTOAsync(int orderId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<OrderDTO>(
                $"api/orders/getOrderByOrderId/{orderId}"
            );

            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<OrderDTO>> GetMyOrdersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<OrderDTO>>("api/orders/my");
            return result ?? new List<OrderDTO>();
        }
        catch
        {
            return new List<OrderDTO>();
        }
    }








}












