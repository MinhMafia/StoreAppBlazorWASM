using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StoreApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register Services
builder.Services.AddScoped<IProductClientService, ProductClientService>();
builder.Services.AddScoped<ICategoryClientService, CategoryClientService>();
builder.Services.AddScoped<ICustomerClientService, CustomerClientService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

// Configure HttpClient with API base address (tự động lấy từ host)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();
