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
using StoreApp.Services.AI;
using StoreApp.Services.AI.Embeddings;
using StoreApp.Services.AI.VectorStore;
using StoreApp.Services.AI.SemanticSearch;
using StoreApp.Middlewares;
using StoreApp.Components;
using StoreApp.Client.Services;
using Blazored.LocalStorage;

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

// DbContext Factory for parallel operations (AI Semantic Indexing)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
    ServiceLifetime.Scoped
);

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
builder.Services.AddScoped<AiRepository>();
builder.Services.AddScoped<ReportsRepository>();

// --- Phần Backend cũ: Register Services ---
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<StoreApp.Services.PromotionService>();
builder.Services.AddScoped<StoreApp.Services.StatisticsService>();
builder.Services.AddScoped<OrderItemService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<JwtService>();

// 👇👈 ADD THIS — để fix lỗi IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// --- AI Services (Semantic Kernel) ---
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<TokenizerService>();
// Admin/Staff AI - Semantic Kernel
builder.Services.AddScoped<SemanticKernelService>();
// Customer AI - Semantic Kernel
builder.Services.AddScoped<CustomerSemanticKernelService>();

// --- AI Semantic Search Services (Qdrant + Embedding) ---
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IVectorStoreService, QdrantVectorStoreService>();
builder.Services.AddScoped<ISemanticSearchService, ProductSemanticSearchService>();
builder.Services.AddScoped<IProductIndexingService, ProductIndexingService>();

// [AUTO-INDEX] Tự động index Products khi server khởi động 
// builder.Services.AddHostedService<SemanticIndexingHostedService>();

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

// --- Blazor Client Services (cho prerendering) ---
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IStatisticsService, StoreApp.Client.Services.StatisticsService>();
builder.Services.AddScoped<IPromotionService, StoreApp.Client.Services.PromotionService>();
builder.Services.AddScoped<IProductClientService, ProductClientService>();
builder.Services.AddScoped<ICategoryClientService, CategoryClientService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<ICustomerAiChatService, CustomerAiChatService>();
builder.Services.AddScoped<IOrdersClientService, OrdersClientService>();
builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();



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