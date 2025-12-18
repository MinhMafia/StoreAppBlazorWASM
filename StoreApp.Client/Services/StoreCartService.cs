using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
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

    private async Task<ProductDTO?> FetchProductAsync(int productId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDTO>($"api/products/{productId}");
        }
        catch
        {
            return null;
        }
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

    private async Task<List<CartItemRequest>> LoadStorageItemsAsync()
    {
        try
        {
            var cartJson = await _localStorage.GetItemAsStringAsync(CART_STORAGE_KEY);
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItemRequest>();

            return JsonSerializer.Deserialize<List<CartItemRequest>>(cartJson) ?? new List<CartItemRequest>();
        }
        catch
        {
            return new List<CartItemRequest>();
        }
    }

    private async Task SaveCartToStorage(List<CartItemRequest> cartItems)
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

    private async Task SaveCartAndBroadcast(List<CartItemRequest> cartItems, bool syncToServer)
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

    private async Task<List<CartItemDTO>> BuildCartItemsAsync(List<CartItemRequest> storageItems)
    {
        var result = new List<CartItemDTO>();
        var shouldResave = false;

        foreach (var storageItem in storageItems)
        {
            var quantity = Math.Max(storageItem.Quantity, 0);
            if (quantity != storageItem.Quantity)
            {
                storageItem.Quantity = quantity;
                shouldResave = true;
            }

            var product = await FetchProductAsync(storageItem.ProductId);
            if (product == null)
            {
                result.Add(new CartItemDTO
                {
                    ProductId = storageItem.ProductId,
                    ProductName = "Sản phẩm không còn tồn tại",
                    ImageUrl = "/assets/images/products/product.jpg",
                    Price = 0,
                    Quantity = quantity,
                    AvailableQuantity = 0
                });
                continue;
            }

            var available = product.Inventory?.Quantity;
            var normalizedAvailable = available.HasValue ? Math.Max(available.Value, 0) : (int?)null;

            if (normalizedAvailable.HasValue && normalizedAvailable.Value > 0 && normalizedAvailable.Value < quantity)
            {
                quantity = normalizedAvailable.Value;
                storageItem.Quantity = quantity;
                shouldResave = true;
            }

            result.Add(new CartItemDTO
            {
                ProductId = storageItem.ProductId,
                ProductName = product.ProductName,
                ImageUrl = string.IsNullOrEmpty(product.ImageUrl) ? "/assets/images/products/product.jpg" : product.ImageUrl!,
                Price = product.Price,
                Quantity = quantity,
                AvailableQuantity = normalizedAvailable
            });
        }

        if (shouldResave)
        {
            await SaveCartToStorage(storageItems);
        }

        return result;
    }

    public async Task AddToCart(int productId, string productName, string imageUrl, decimal price, int quantity = 1, int? availableQuantity = null)
    {
        var storageItems = await LoadStorageItemsAsync();
        var existingItem = storageItems.FirstOrDefault(x => x.ProductId == productId);

        var product = await FetchProductAsync(productId);
        var available = product?.Inventory?.Quantity;
        var normalizedAvailable = available.HasValue ? Math.Max(available.Value, 0) : (int?)null;

        var desiredQuantity = (existingItem?.Quantity ?? 0) + Math.Max(quantity, 0);
        if (normalizedAvailable.HasValue)
        {
            desiredQuantity = Math.Min(desiredQuantity, normalizedAvailable.Value);
        }

        if (desiredQuantity <= 0)
        {
            storageItems.RemoveAll(x => x.ProductId == productId);
        }
        else if (existingItem != null)
        {
            existingItem.Quantity = desiredQuantity;
        }
        else
        {
            storageItems.Add(new CartItemRequest
            {
                ProductId = productId,
                Quantity = desiredQuantity
            });
        }

        await SaveCartAndBroadcast(storageItems, syncToServer: true);
    }

    public async Task RemoveFromCart(int productId)
    {
        var storageItems = await LoadStorageItemsAsync();
        storageItems.RemoveAll(x => x.ProductId == productId);
        await SaveCartAndBroadcast(storageItems, syncToServer: true);
    }

    public async Task UpdateQuantity(int productId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveFromCart(productId);
            return;
        }

        var storageItems = await LoadStorageItemsAsync();
        var item = storageItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            var product = await FetchProductAsync(productId);
            var available = product?.Inventory?.Quantity;
            var normalizedAvailable = available.HasValue ? Math.Max(available.Value, 0) : (int?)null;

            var newQuantity = quantity;
            if (normalizedAvailable.HasValue)
            {
                newQuantity = Math.Min(quantity, normalizedAvailable.Value);
            }

            if (newQuantity <= 0)
            {
                storageItems.RemoveAll(x => x.ProductId == productId);
            }
            else
            {
                item.Quantity = newQuantity;
            }

            await SaveCartAndBroadcast(storageItems, syncToServer: true);
        }
    }

    public async Task<List<CartItemDTO>> GetCartItems()
    {
        var storageItems = await LoadStorageItemsAsync();
        return await BuildCartItemsAsync(storageItems);
    }

    public async Task ClearCart()
    {
        await SaveCartAndBroadcast(new List<CartItemRequest>(), syncToServer: true);
    }

    public async Task ClearLocalCart()
    {
        await SaveCartToStorage(new List<CartItemRequest>());
        await RaiseCartChangedAsync();
    }

    public async Task<int> GetCartItemCount()
    {
        var storageItems = await LoadStorageItemsAsync();
        return storageItems.Where(x => x.Quantity > 0).Sum(x => x.Quantity);
    }

    public async Task RemovePurchasedItemsFromCart(IEnumerable<int> purchasedProductIds)
    {
        if (purchasedProductIds == null || !purchasedProductIds.Any())
            return;

        var storageItems = await LoadStorageItemsAsync();
        storageItems.RemoveAll(item => purchasedProductIds.Contains(item.ProductId));
        await SaveCartAndBroadcast(storageItems, syncToServer: true);
    }

    public async Task SyncToServerAsync()
    {
        if (!await HasCustomerTokenAsync()) return;
        try
        {
            var items = await LoadStorageItemsAsync();
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
            var localItems = await LoadStorageItemsAsync();
            var overrideFromLocal = await _localStorage.GetItemAsync<bool>("redirectAfterLoginCart");

            // Only override server cart when login was triggered from checkout button
            if (overrideFromLocal && localItems.Any())
            {
                await _httpClient.PostAsJsonAsync("api/cart/sync", localItems);
                await SaveCartToStorage(localItems);
                await _localStorage.RemoveItemAsync("redirectAfterLoginCart");
            }
            else
            {
                var serverItems = await _httpClient.GetFromJsonAsync<List<CartItemRequest>>("api/cart") ?? new List<CartItemRequest>();
                await SaveCartToStorage(serverItems);
            }

            await RaiseCartChangedAsync();
        }
        catch
        {
        }
    }
}
