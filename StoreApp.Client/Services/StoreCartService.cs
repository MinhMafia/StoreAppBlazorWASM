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
    Task<int> GetCartItemCount();
    Task SyncToServerAsync();
    Task PullFromServerAsync();
    Task RemovePurchasedItemsFromCart(IEnumerable<int> purchasedProductIds);

    event Action? OnCartChanged;
}

public class StoreCartService : IStoreCartService
{
    private const string CART_STORAGE_KEY = "store_cart";
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public event Action? OnCartChanged;

    public StoreCartService(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    private async Task<bool> HasTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            return !string.IsNullOrWhiteSpace(token);
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

        await SaveCartToStorage(cartItems);
        OnCartChanged?.Invoke();
    }

    public async Task RemoveFromCart(int productId)
    {
        var cartItems = await LoadCartFromStorage();
        cartItems.RemoveAll(x => x.ProductId == productId);
        await SaveCartToStorage(cartItems);
        OnCartChanged?.Invoke();
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
            await SaveCartToStorage(cartItems);
            OnCartChanged?.Invoke();
        }
    }

    public async Task<List<CartItemDTO>> GetCartItems()
    {
        return await LoadCartFromStorage();
    }

    public async Task ClearCart()
    {
        await _localStorage.RemoveItemAsync(CART_STORAGE_KEY);
        OnCartChanged?.Invoke();
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
        await SaveCartToStorage(cartItems);
        OnCartChanged?.Invoke();
    }

    public async Task SyncToServerAsync()
    {
        if (!await HasTokenAsync()) return;
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
        if (!await HasTokenAsync()) return;
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<CartItemDTO>>("api/cart") ?? new List<CartItemDTO>();
            await SaveCartToStorage(items);
            OnCartChanged?.Invoke();
        }
        catch
        {
        }
    }
}
