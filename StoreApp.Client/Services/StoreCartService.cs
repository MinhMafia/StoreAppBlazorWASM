using StoreApp.Shared;

namespace StoreApp.Client.Services;

public interface IStoreCartService
{
    Task AddToCart(int productId, int quantity);
    Task RemoveFromCart(int productId);
    Task UpdateQuantity(int productId, int quantity);
    Task<List<CartItemDTO>> GetCartItems();
    Task ClearCart();
    int GetCartItemCount();
    event Action? OnCartChanged;
}

public class StoreCartService : IStoreCartService
{
    private List<CartItemDTO> _cartItems = new();
    private readonly HttpClient _httpClient;

    public event Action? OnCartChanged;

    public StoreCartService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task AddToCart(int productId, int quantity)
    {
        // Implement logic - thêm sản phẩm vào giỏ hàng
        // Có thể lưu vào localStorage hoặc state management
        OnCartChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task RemoveFromCart(int productId)
    {
        // Implement logic - xóa sản phẩm khỏi giỏ hàng
        _cartItems.RemoveAll(x => x.ProductId == productId);
        OnCartChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task UpdateQuantity(int productId, int quantity)
    {
        // Implement logic - cập nhật số lượng
        var item = _cartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            item.Quantity = quantity;
        }
        OnCartChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task<List<CartItemDTO>> GetCartItems()
    {
        // Implement logic - lấy danh sách items từ localStorage hoặc state
        return Task.FromResult(_cartItems);
    }

    public Task ClearCart()
    {
        // Implement logic - xóa toàn bộ giỏ hàng
        _cartItems.Clear();
        OnCartChanged?.Invoke();
        return Task.CompletedTask;
    }

    public int GetCartItemCount()
    {
        return _cartItems.Sum(x => x.Quantity);
    }
}

// DTO cho Cart Item (sử dụng ProductDTO và thêm Quantity)
public class CartItemDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Price * Quantity;
}

