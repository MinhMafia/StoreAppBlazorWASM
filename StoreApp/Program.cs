using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoreApp.Data;
using StoreApp.Repository;
using StoreApp.Services;
using StoreApp.Middlewares;
using StoreApp.Components;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIG SERVICES (CẤU HÌNH DỊCH VỤ)
// ==========================================

// --- Phần bắt buộc của Blazor ---
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// --- Phần Backend cũ: Controllers & JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// --- Phần Backend cũ: Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Phần Backend cũ: Database ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// --- Phần Backend cũ: Register Repositories ---
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<ActivityLogRepository>();
builder.Services.AddScoped<PromotionRepository>();
builder.Services.AddScoped<StatisticsRepository>();
builder.Services.AddScoped<OrderItemRepository>();
builder.Services.AddScoped<InventoryRepository>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<SupplierRepository>();

// --- Phần Backend cũ: Register Services ---
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<OrderItemService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ImportService>();
// builder.Services.AddScoped<CategoryService>(); // Bị trùng, xóa bớt
// builder.Services.AddScoped<SupplierService>(); // Bị trùng, xóa bớt
builder.Services.AddScoped<JwtService>();

// --- Phần Backend cũ: Upload Limit ---
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
});

// --- Phần Backend cũ: Authentication JWT ---
var key = builder.Configuration["Jwt:Key"];
// Kiểm tra null để tránh lỗi crash nếu chưa config appsettings
if (!string.IsNullOrEmpty(key))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
});

var app = builder.Build();

// ==========================================
// 2. PIPELINE (LUỒNG XỬ LÝ REQUEST)
// ==========================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging(); // Cần thiết cho Blazor debug
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// --- Middleware của bạn ---
// Đặt trước StaticFiles để log mọi thứ, hoặc sau StaticFiles để chỉ log API
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseStaticFiles(); // Serve file trong wwwroot (bao gồm ảnh sản phẩm)
app.UseAntiforgery(); // Bảo mật CSRF của Blazor

// --- Auth Middleware ---
app.UseAuthentication();
app.UseAuthorization();

// --- Map Endpoints ---

// 1. Map Controllers (Cho API Backend cũ chạy)
app.MapControllers();

// 2. Map Blazor (Cho giao diện mới chạy)
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StoreApp.Client._Imports).Assembly);

app.Run();