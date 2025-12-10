using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StoreApp.Client.Services;
using StoreApp.Client.Middlewares;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components; 
using System.Net.Http; 

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// LocalStorage (Nên đặt lên đầu vì các service khác cần nó)
builder.Services.AddBlazoredLocalStorage();
// --- 1. ĐĂNG KÝ HANDLER ---
builder.Services.AddScoped<JwtAuthorizationMessageHandler>();

// --- 2. CẤU HÌNH HTTP CLIENT CHO TẤT CẢ CÁC SERVICE CLIENT CẦN AUTH ---

// Hàm tiện ích để cấu hình
void AddHttpClientWithAuth<TInterface, TImplementation>() 
    where TInterface : class 
    where TImplementation : class, TInterface
{
    builder.Services.AddHttpClient<TInterface, TImplementation>(client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();
}

// Register Services => CODE CŨ
// builder.Services.AddScoped<IProductClientService, ProductClientService>();
// builder.Services.AddScoped<ICategoryClientService, CategoryClientService>();
// builder.Services.AddScoped<ICustomerClientService, CustomerClientService>();
// builder.Services.AddScoped<IStatisticsService, StatisticsService>();
// builder.Services.AddScoped<IPromotionService, PromotionService>();
// builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();
// builder.Services.AddScoped<IOrdersClientService, OrdersClientService>();

// Test xem có gắn header chưa
AddHttpClientWithAuth<IProductClientService, ProductClientService>();
AddHttpClientWithAuth<ICategoryClientService, CategoryClientService>();
AddHttpClientWithAuth<ICustomerClientService, CustomerClientService>();
AddHttpClientWithAuth<IStatisticsService, StatisticsService>();
AddHttpClientWithAuth<IPromotionService, PromotionService>();
AddHttpClientWithAuth<ICustomerAuthService, CustomerAuthService>();
AddHttpClientWithAuth<IOrdersClientService, OrdersClientService>(); 

// Giữ nguyên vì không cần gắn header
// AI Chat Service
builder.Services.AddScoped<IAiChatService, AiChatService>();

// Customer AI Chat Service
builder.Services.AddScoped<ICustomerAiChatService, CustomerAiChatService>();

// Auth Service
builder.Services.AddScoped<IAuthService, AuthService>();


// LocalStorage => Code cũ
// builder.Services.AddBlazoredLocalStorage();

// Code cũ => Tránh xung đột
// Configure HttpClient with API base address (tự động lấy từ host)
// builder.Services.AddScoped(sp => new HttpClient
// {
//     BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
// });

// await builder.Build().RunAsync();

// ⭐ PHẦN KHẮC PHỤC LỖI BASEADDRESS CHO HttpClient MẶC ĐỊNH (Sử dụng cho Login.razor)
builder.Services.AddScoped(sp => new HttpClient
{
    // Sử dụng NavigationManager để lấy BaseUri
    BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
});

await builder.Build().RunAsync();
