using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using StoreApp.Shared;

namespace StoreApp.Client.Services;

public interface IStoreCartService
{
    Task AddToCart(int productId, string productName, string imageUrl, decimal price, int quantity = 1, int? availableQuantity = null);
    Task RemoveFromCart(int productId);
    Task UpdateQuantity(int productId, int quantity);
    Task<List<CartItemDTO>> GetCartItems();
    Task ClearCart();
    Task ClearLocalCart(); // clear local without pushing empty cart to server (useful on logout)
    Task<int> GetCartItemCount();
    Task SyncToServerAsync();
    Task PullFromServerAsync();
    Task RemovePurchasedItemsFromCart(IEnumerable<int> purchasedProductIds);

    event Func<Task>? OnCartChanged;
}

public class StoreCartService : IStoreCartService
{
    private const string CART_STORAGE_KEY = "store_cart";
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public event Func<Task>? OnCartChanged;

    public StoreCartService(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    private async Task RaiseCartChangedAsync()
    {
        if (OnCartChanged == null) return;
        foreach (var handler in OnCartChanged.GetInvocationList().Cast<Func<Task>>())
        {
            try { await handler(); } catch { }
        }
    }

    private async Task<bool> HasCustomerTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            if (string.IsNullOrWhiteSpace(token)) return false;

            var role = await _localStorage.GetItemAsStringAsync("userRole");
            return string.Equals(role?.Trim('"'), "customer", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<CartItemDTO>> LoadCartFromStorage()
    {
        try
        {
            var cartJson = await _localStorage.GetItemAsStringAsync(CART_STORAGE_KEY);
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItemDTO>();

            return JsonSerializer.Deserialize<List<CartItemDTO>>(cartJson) ?? new List<CartItemDTO>();
        }
        catch
        {
            return new List<CartItemDTO>();
        }
    }

    private async Task SaveCartToStorage(List<CartItemDTO> cartItems)
    {
        try
        {
            var cartJson = JsonSerializer.Serialize(cartItems);
            await _localStorage.SetItemAsStringAsync(CART_STORAGE_KEY, cartJson);
        }
        catch
        {
        }
    }

    private async Task SaveCartAndBroadcast(List<CartItemDTO> cartItems, bool syncToServer)
    {
        await SaveCartToStorage(cartItems);
        await RaiseCartChangedAsync();

        if (syncToServer && await HasCustomerTokenAsync())
        {
            try
            {
                await _httpClient.PostAsJsonAsync("api/cart/sync", cartItems);
            }
            catch
            {
            }
        }
    }

    public async Task AddToCart(int productId, string productName, string imageUrl, decimal price, int quantity = 1, int? availableQuantity = null)
    {
        var cartItems = await LoadCartFromStorage();
        var existingItem = cartItems.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (existingItem.AvailableQuantity.HasValue && newQuantity > existingItem.AvailableQuantity.Value)
            {
                newQuantity = existingItem.AvailableQuantity.Value;
            }
            existingItem.Quantity = newQuantity;
        }
        else
        {
            cartItems.Add(new CartItemDTO
            {
                ProductId = productId,
                ProductName = productName,
                ImageUrl = imageUrl,
                Price = price,
                Quantity = quantity,
                AvailableQuantity = availableQuantity
            });
        }

        await SaveCartAndBroadcast(cartItems, syncToServer: true);
    }

    public async Task RemoveFromCart(int productId)
    {
        var cartItems = await LoadCartFromStorage();
        cartItems.RemoveAll(x => x.ProductId == productId);
        await SaveCartAndBroadcast(cartItems, syncToServer: true);
    }

    public async Task UpdateQuantity(int productId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveFromCart(productId);
            return;
        }

        var cartItems = await LoadCartFromStorage();
        var item = cartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            if (item.AvailableQuantity.HasValue && quantity > item.AvailableQuantity.Value)
            {
                quantity = item.AvailableQuantity.Value;
            }
            item.Quantity = quantity;
            await SaveCartAndBroadcast(cartItems, syncToServer: true);
        }
    }

    public async Task<List<CartItemDTO>> GetCartItems()
    {
        return await LoadCartFromStorage();
    }

    public async Task ClearCart()
    {
        await SaveCartAndBroadcast(new List<CartItemDTO>(), syncToServer: true);
    }

    public async Task ClearLocalCart()
    {
        await SaveCartToStorage(new List<CartItemDTO>());
        await RaiseCartChangedAsync();
    }

    public async Task<int> GetCartItemCount()
    {
        var cartItems = await LoadCartFromStorage();
        return cartItems.Sum(x => x.Quantity);
    }

    public async Task RemovePurchasedItemsFromCart(IEnumerable<int> purchasedProductIds)
    {
        if (purchasedProductIds == null || !purchasedProductIds.Any())
            return;

        var cartItems = await LoadCartFromStorage();
        cartItems.RemoveAll(item => purchasedProductIds.Contains(item.ProductId));
        await SaveCartAndBroadcast(cartItems, syncToServer: true);
    }

    public async Task SyncToServerAsync()
    {
        if (!await HasCustomerTokenAsync()) return;
        try
        {
            var items = await LoadCartFromStorage();
            await _httpClient.PostAsJsonAsync("api/cart/sync", items);
        }
        catch
        {
        }
    }

    public async Task PullFromServerAsync()
    {
        if (!await HasCustomerTokenAsync()) return;
        try
        {
            var localItems = await LoadCartFromStorage();
            var overrideFromLocal = await _localStorage.GetItemAsync<bool>("redirectAfterLoginCart");

            // Chỉ ghi đè server khi đây là luồng bấm Mua hàng -> login
            if (overrideFromLocal && localItems.Any())
            {
                await _httpClient.PostAsJsonAsync("api/cart/sync", localItems);
                await SaveCartToStorage(localItems);
                await _localStorage.RemoveItemAsync("redirectAfterLoginCart");
            }
            else
            {
                var serverItems = await _httpClient.GetFromJsonAsync<List<CartItemDTO>>("api/cart") ?? new List<CartItemDTO>();
                await SaveCartToStorage(serverItems);
            }

            await RaiseCartChangedAsync();
        }
        catch
        {
        }
    }
}
