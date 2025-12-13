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
    Task<bool> SaveListOrderItem (List<OrderItemReponse> items);
    Task<PaymentResult> PayWithMomoAsync(int orderId, decimal amount);
    Task<PaymentResult> PayOfflineAsync(int orderId, decimal amount);
    Task<bool> HandleCancelClick(int orderId);
    Task<OrderDTO> CreateTemporaryOnlineOrderAsync();
    Task<PaymentResult> PayCashInOnlineOrderAsync(int orderId, decimal amount);
    Task<PaymentResult> PayWithMomoWitOnlineOrderAsync(int orderId, decimal amount);
    Task<OrderDTO?> GetOrderDTOAsync(int orderId);
    
   
    
    

        
                                                  
}

public class OrdersClientService : IOrdersClientService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public OrdersClientService(HttpClient http,IJSRuntime js)
    {
         _http = http;
         _js = js;
    }
        // 1. Táº¡o Ä‘Æ¡n táº¡m cho Ä‘Æ°Æ¡n hÃ ng online(draft) â†’ tráº£ vá» OrderDTO cÃ³ Id + OrderNumber
    public async Task<OrderDTO> CreateTemporaryOnlineOrderAsync()
    {
        var response = await _http.PostAsync("api/orders/createonlineordertemp", null);

        if (!response.IsSuccessStatusCode)
            return new OrderDTO();

        var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return order ?? new OrderDTO();
    }
  


    // 1. Táº¡o Ä‘Æ¡n táº¡m (draft) â†’ tráº£ vá» OrderDTO cÃ³ Id + OrderNumber
    public async Task<OrderDTO> CreateTemporaryOrderAsync()
    {
        var response = await _http.PostAsync("api/orders/create-temp", null);

        if (!response.IsSuccessStatusCode)
            return new OrderDTO();

        var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return order ?? new OrderDTO();
    }

    // 2. LÆ¯U ÄÆ N CHÃNH THá»¨C (má»›i thÃªm) â†’ gá»­i toÃ n bá»™ OrderDTO Ä‘Ã£ chá»‰nh sá»­a lÃªn
    public async Task<OrderDTO?> SaveFinalOrderAsync(OrderDTO orderDto)
    {
        if (orderDto == null) return null;

        var response = await _http.PostAsJsonAsync("api/orders/create", orderDto);

        if (!response.IsSuccessStatusCode)
            return null;

        // API tráº£ vá» true/false dáº¡ng JSON hoáº·c plain text
        var result = await response.Content.ReadFromJsonAsync<OrderDTO>();
        return result;
    }

    // 3. TÃ¬m kiáº¿m + phÃ¢n trang 
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

    // Láº¥y danh sÃ¡ch cÃ¡c OrderItemReponse theo OrderId
    public async Task<List<OrderItemReponse>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _http.GetFromJsonAsync<List<OrderItemReponse>>(
            $"api/orderitem/byorder/{orderId}"
        ) ?? new List<OrderItemReponse>();
    }

    // Láº¥y danh sÃ¡ch Promotion mÃ  khÃ¡ch hÃ ng Ä‘Ã£ sá»­ dá»¥ng trong Ä‘Æ¡n hÃ ng
    public async Task<PromotionDTO?> GetPromotionByIdAsync(int Id)
    {
        try
        {
            // GIáº¢ Sá»¬ PromotionsController cÃ³ route lÃ  "api/promotions"
            return await _http.GetFromJsonAsync<PromotionDTO?>(
                $"api/promotions/{Id}" 
            );
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }


    // Káº¿t há»£p 2 api lÃ  láº¥y danh sÃ¡ch sáº£n pháº©m cÃ³ phÃ¢n trang vÃ  tÃ¬m kiáº¿m sáº£n pháº©m
    public async Task<PaginationResult<ProductDTO>> GetProductsPagedAndSearchedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null)
    {
        // Validate
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        // Trim keyword náº¿u cÃ³
        searchKeyword = searchKeyword?.Trim();

        string url;
        if (string.IsNullOrEmpty(searchKeyword))
        {
            // Chá»‰ phÃ¢n trang
            url = $"api/products/available?page={pageNumber}&pageSize={pageSize}";
        }
        else
        {
            // PhÃ¢n trang + tÃ¬m kiáº¿m
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

    // Láº¥y cÃ¡c khuyáº¿n mÃ£i Ä‘ang hoáº¡t Ä‘á»™ng
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

    // Luu danh sach order item
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

            // Náº¿u response null hoáº·c status code lá»—i â†’ return false
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

            // 2) Gá»i API create
            var res = await _http.PostAsJsonAsync("api/payment/momo/create", body);

            if (!res.IsSuccessStatusCode)
                return new PaymentResult { Success = false, Message = "KhÃ´ng táº¡o Ä‘Æ°á»£c payment MoMo" };

            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json == null || !json.ContainsKey("payUrl"))
                return new PaymentResult { Success = false, Message = "Thiáº¿u payUrl" };

            string payUrl = json["payUrl"].ToString()!;

            
            await _js.InvokeVoidAsync("openMoMoPayment", payUrl);

            // 4) Polling â†’ giá»‘ng JS
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
                                Message = "Thanh toÃ¡n thÃ nh cÃ´ng!"
                            };
                        }
                    }
                }

                // Timeout 60 láº§n â†’ 2 phÃºt
                if (counter >= 60)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "QuÃ¡ thá»i gian chá» thanh toÃ¡n"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lá»—i client: " + ex.Message
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
                return new PaymentResult { Success = false, Message = "KhÃ´ng táº¡o Ä‘Æ°á»£c payment MoMo" };

            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json == null || !json.ContainsKey("payUrl"))
                return new PaymentResult { Success = false, Message = "Thiáº¿u payUrl" };

            string payUrl = json["payUrl"].ToString()!;

            
            await _js.InvokeVoidAsync("openMoMoPayment", payUrl);

            // 4) Polling â†’ giá»‘ng JS
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
                                Message = "Thanh toÃ¡n thÃ nh cÃ´ng!"
                            };
                        }
                    }
                }

                // Timeout 60 láº§n â†’ 2 phÃºt
                if (counter >= 60)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "QuÃ¡ thá»i gian chá» thanh toÃ¡n"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lá»—i client: " + ex.Message
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
                return new PaymentResult { Success = false, Message = "KhÃ´ng táº¡o Ä‘Æ°á»£c offline payment" };

            return new PaymentResult
            {
                Success = true,
                Message = "Thanh toÃ¡n tiá»n máº·t thÃ nh cÃ´ng!"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lá»—i offline payment: " + ex.Message
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
                return new PaymentResult { Success = false, Message = "KhÃ´ng táº¡o Ä‘Æ°á»£c offline payment" };

            return new PaymentResult
            {
                Success = true,
                Message = "Thanh toÃ¡n tiá»n máº·t thÃ nh cÃ´ng!"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Lá»—i offline payment: " + ex.Message
            };
        }
    }

    //Xá»­ lÃ­ Ä‘Æ¡n hÃ ng
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

// Láº¥y Ä‘Æ¡n hÃ ng theo orderId
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









}














