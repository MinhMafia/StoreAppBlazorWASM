# Chức năng Quản lý Nhân viên

## 📋 Tổng quan

Hệ thống quản lý nhân viên cho phép Admin xem, chỉnh sửa, khóa/mở khóa tài khoản và đặt lại mật khẩu cho nhân viên.

---

## 🎯 Các chức năng chính

### 1. **Xem danh sách nhân viên**
- Hiển thị danh sách với phân trang
- Thông tin: Email, Username, Vai trò, Trạng thái
- Hỗ trợ phân trang với meta data

### 2. **Xem chi tiết nhân viên**
- Modal hiển thị đầy đủ thông tin
- Các trường: Họ tên, Email, Username, Vai trò, Trạng thái, Ngày tạo, Đăng nhập cuối

### 3. **Chỉnh sửa nhân viên**
- Sửa họ tên và vai trò (Staff/Admin)
- Validation form
- Hiển thị lỗi trong modal

### 4. **Đặt lại mật khẩu**
- Admin có thể reset password cho nhân viên
- Yêu cầu xác nhận mật khẩu
- Validation: tối thiểu 6 ký tự, khớp nhau

### 5. **Khóa/Mở khóa tài khoản**
- Toggle trạng thái active/inactive
- Xác nhận trước khi thực hiện
- Cập nhật realtime

---

## 🏗️ Kiến trúc

### Frontend (Blazor WebAssembly)

#### **1. Page Component**
```razor
// filepath: StoreApp.Client/Pages/Admin/UserManagement.razor
@page "/admin/users"
@layout Layout.MainLayout
@inject IUserClientService UserService
@inject IJSRuntime JS

<div class="container py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-start mb-3">
        <div>
            <h1 class="h3 fw-bold">Quản lý nhân viên</h1>
            <p class="text-muted">Xem chi tiết, sửa, khóa/mở và đặt lại mật khẩu</p>
        </div>
    </div>

    <!-- Alert -->
    @if (!string.IsNullOrWhiteSpace(AlertMessage) && !ShowEditModal && !ShowResetModal)
    {
        <div class="alert @AlertCss">@AlertMessage</div>
    }

    <!-- Table -->
    <table class="table">
        <thead>
            <tr>
                <th>Email</th>
                <th>Username</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Hành động</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in Users)
            {
                <tr>
                    <td>@user.Email</td>
                    <td>@user.Username</td>
                    <td><span class="badge @RoleBadge(user.Role)">@user.Role</span></td>
                    <td><span class="badge @StatusBadge(user.Active)">...</span></td>
                    <td>
                        <button @onclick="() => OpenDetail(user)">Chi tiết</button>
                        <button @onclick="() => OpenEditModal(user)">Sửa</button>
                        <button @onclick="() => OpenResetModal(user)">Đặt lại MK</button>
                        <button @onclick="() => ToggleStatus(user)">Khóa/Mở</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <!-- Pagination -->
    @if (Meta.TotalPages > 1)
    {
        <nav>
            <button @onclick="PrevPage" disabled="@(Meta.CurrentPage == 1)">Trước</button>
            <span>Trang @Meta.CurrentPage / @Meta.TotalPages</span>
            <button @onclick="NextPage" disabled="@(Meta.CurrentPage >= Meta.TotalPages)">Sau</button>
        </nav>
    }
</div>

@code {
    private List<UserDTO> Users = new();
    private PaginationResult<UserDTO> Meta = new();
    private bool IsLoading = true;
    private string AlertMessage = string.Empty;
    private string AlertCss = "alert-info";

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers(1);
    }

    private async Task LoadUsers(int page)
    {
        IsLoading = true;
        var result = await UserService.GetStaffsAsync(page, 10);
        Users = result.Items.ToList();
        Meta = result;
        IsLoading = false;
    }
}
```

#### **2. Service Client**
```csharp
// filepath: StoreApp.Client/Services/UserClientService.cs
public interface IUserClientService
{
    Task<PaginationResult<UserDTO>> GetStaffsAsync(int page, int pageSize);
    Task<UserDTO> GetUserByIdAsync(int id);
    Task<UserDTO> UpdateUserAsync(int id, UpdateUserDTO dto);
    Task ResetPasswordAsync(int id, string newPassword);
    Task ToggleUserStatusAsync(int id, bool isActive);
}

public class UserClientService : IUserClientService
{
    private readonly HttpClient _http;
    private readonly ILogger<UserClientService> _logger;

    public UserClientService(HttpClient http, ILogger<UserClientService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PaginationResult<UserDTO>> GetStaffsAsync(int page, int pageSize)
    {
        var url = $"api/users/staffs?page={page}&pageSize={pageSize}";
        var response = await _http.GetFromJsonAsync<PaginationResult<UserDTO>>(url);
        return response ?? new PaginationResult<UserDTO>();
    }

    public async Task<UserDTO> UpdateUserAsync(int id, UpdateUserDTO dto)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDTO>() 
            ?? throw new Exception("Failed to update user");
    }

    public async Task ResetPasswordAsync(int id, string newPassword)
    {
        // Lấy user hiện tại
        var user = await GetUserByIdAsync(id);
        
        // Tạo DTO với password mới
        var updateDto = new UpdateUserDTO
        {
            FullName = user.FullName,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            Password = newPassword, // Password mới
            Active = user.Active
        };
        
        await UpdateUserAsync(id, updateDto);
    }

    public async Task ToggleUserStatusAsync(int id, bool isActive)
    {
        var response = await _http.PatchAsync(
            $"api/users/{id}/status?isActive={isActive}", null);
        response.EnsureSuccessStatusCode();
    }
}
```

#### **3. Đăng ký Service**
```csharp
// filepath: StoreApp.Client/Program.cs
// ...existing code...

void AddHttpClientWithAuth<TInterface, TImplementation>()
    where TInterface : class
    where TImplementation : class, TInterface
{
    builder.Services.AddHttpClient<TInterface, TImplementation>(client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>(); // ← Tự động gắn JWT
}

// Đăng ký UserClientService
AddHttpClientWithAuth<IUserClientService, UserClientService>();

// ...existing code...
```

---

### Backend (ASP.NET Core Web API)

#### **1. Controller**
```csharp
// filepath: StoreApp/Controllers/UsersController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")] // Chỉ admin mới truy cập
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpGet("staffs")]
    public async Task<IActionResult> GetStaffs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetStaffsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO dto)
    {
        var updated = await _userService.UpdateUserAsync(id, dto);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] bool isActive)
    {
        var updated = await _userService.UpdateUserStatusAsync(id, isActive);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
```

#### **2. Service Layer**
```csharp
// filepath: StoreApp/Services/UserService.cs
public interface IUserService
{
    Task<PaginationResult<UserDTO>> GetStaffsAsync(int page, int pageSize);
    Task<UserDTO?> GetUserByIdAsync(int id);
    Task<UserDTO?> UpdateUserAsync(int id, UpdateUserDTO dto);
    Task<UserDTO?> UpdateUserStatusAsync(int id, bool isActive);
    Task DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public async Task<PaginationResult<UserDTO>> GetStaffsAsync(int page, int pageSize)
    {
        var query = _context.Users
            .Where(u => u.Role == "staff" || u.Role == "admin")
            .OrderByDescending(u => u.CreatedAt);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username,
                Role = u.Role,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin
            })
            .ToListAsync();

        return new PaginationResult<UserDTO>
        {
            Items = items,
            TotalItems = totalItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
    }

    public async Task<UserDTO?> UpdateUserAsync(int id, UpdateUserDTO dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        // Cập nhật thông tin
        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.Username = dto.Username;
        user.Role = dto.Role;
        user.Active = dto.Active;

        // Cập nhật password nếu có
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        
        return new UserDTO { ... };
    }

    public async Task<UserDTO?> UpdateUserStatusAsync(int id, bool isActive)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.Active = isActive;
        await _context.SaveChangesAsync();

        return new UserDTO { ... };
    }
}
```

#### **3. DTOs**
```csharp
// filepath: StoreApp.Shared/DTOs/UserDTO.cs
public class UserDTO
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UpdateUserDTO
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool Active { get; set; }
}

public class PaginationResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## 🔐 Bảo mật

### **1. JWT Authentication**
```csharp
// filepath: StoreApp.Client/Middlewares/JwtAuthorizationMessageHandler.cs
public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        
        if (!string.IsNullOrWhiteSpace(token))
        {
            token = token.Trim('"');
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### **2. Authorization Policy**
- **Frontend**: Chỉ Admin mới truy cập `/admin/users`
- **Backend**: `[Authorize(Roles = "admin")]` trên controller
- **JWT Claims**: Token chứa `role` claim để xác thực

---

## 📊 Flow hoạt động

### **1. Load danh sách nhân viên**
```
User → UserManagement.razor → LoadUsers()
  ↓
IUserClientService.GetStaffsAsync(page, pageSize)
  ↓
HTTP GET /api/users/staffs?page=1&pageSize=10
  + Header: Authorization: Bearer <JWT_TOKEN>
  ↓
UsersController.GetStaffs()
  → Check [Authorize(Roles = "admin")]
  → UserService.GetStaffsAsync()
  → Query Database
  ↓
Return PaginationResult<UserDTO>
```

### **2. Chỉnh sửa nhân viên**
```
User clicks "Sửa" → OpenEditModal(user)
  ↓
User edits form → SubmitEditAsync()
  ↓
Validation (FullName not empty, Role valid)
  ↓
IUserClientService.UpdateUserAsync(id, dto)
  ↓
HTTP PUT /api/users/{id}
  + Body: { FullName, Email, Username, Role, Active }
  + Header: Authorization: Bearer <JWT_TOKEN>
  ↓
UsersController.UpdateUser()
  → UserService.UpdateUserAsync()
  → Update database
  ↓
Return updated UserDTO
  ↓
Reload list + Show success alert
```

### **3. Đặt lại mật khẩu**
```
User clicks "Đặt lại MK" → OpenResetModal(user)
  ↓
User enters password → SubmitResetAsync()
  ↓
Validation (min 6 chars, passwords match)
  ↓
IUserClientService.ResetPasswordAsync(id, newPassword)
  ↓
  1. GET /api/users/{id} → Get current user data
  2. PUT /api/users/{id} → Update with new hashed password
  ↓
Return success
  ↓
Show alert "Đặt lại mật khẩu thành công"
```

### **4. Khóa/Mở khóa tài khoản**
```
User clicks "Khóa/Mở" → ToggleStatus(user)
  ↓
Confirm dialog → await ConfirmAsync("Bạn có chắc?")
  ↓
IUserClientService.ToggleUserStatusAsync(id, !user.Active)
  ↓
HTTP PATCH /api/users/{id}/status?isActive=false
  + Header: Authorization: Bearer <JWT_TOKEN>
  ↓
UsersController.UpdateStatus()
  → UserService.UpdateUserStatusAsync()
  → Update database: user.Active = isActive
  ↓
Reload list + Show success alert
```

---

## 🎨 UI/UX Features

### **1. Modal States**
- **Detail Modal**: Chỉ xem (read-only)
- **Edit Modal**: Sửa họ tên + vai trò (không sửa password/status)
- **Reset Password Modal**: Chỉ đổi mật khẩu

### **2. Alert Handling**
```razor
@if (!string.IsNullOrWhiteSpace(AlertMessage) && !ShowEditModal && !ShowResetModal)
{
    <div class="alert @AlertCss">@AlertMessage</div>
}
```
- ✅ Lỗi validation → Hiển thị **TRONG modal**
- ✅ Thành công/Thất bại → Hiển thị **NGOÀI** (sau khi đóng modal)

### **3. Badges**
```csharp
private static string RoleBadge(string? role) => role?.ToLower() switch
{
    "admin" => "bg-danger",
    "staff" => "bg-primary",
    _ => "bg-secondary"
};

private static string StatusBadge(bool active) => active 
    ? "bg-success" 
    : "bg-secondary";
```

---

## ✅ Các vấn đề đã sửa

### **1. Lỗi 401 Unauthorized khi gọi API**
- **Nguyên nhân**: Inject `HttpClient` trực tiếp không có JWT handler
- **Giải pháp**: Dùng `IHttpClientFactory.CreateClient("ApiWithAuth")`

### **2. Alert hiển thị ngoài modal thay vì trong**
- **Nguyên nhân**: Dùng chung 1 biến `AlertMessage` cho cả trang và modal
- **Giải pháp**: 
  - Alert **TRONG modal**: Hiển thị khi modal đang mở
  - Alert **NGOÀI**: Chỉ hiển thị khi không có modal nào mở

### **3. Không toggle được trạng thái**
- **Nguyên nhân**: API endpoint sai `PUT /api/users/{id}/toggle`
- **Giải pháp**: Đổi thành `PATCH /api/users/{id}/status?isActive={value}`

### **4. Không reset được password**
- **Nguyên nhân**: Chỉ gửi password, thiếu các field khác
- **Giải pháp**: Lấy user hiện tại → update password → gửi đầy đủ DTO

### **5. Modal Edit trùng lặp chức năng**
- **Nguyên nhân**: Modal có cả password và status (đã có nút riêng)
- **Giải pháp**: Bỏ 2 field này, chỉ giữ họ tên + vai trò

---

## 🚀 Cải tiến trong tương lai

1. **Tìm kiếm nhân viên**: Thêm search box theo email/username
2. **Lọc theo vai trò**: Dropdown lọc Staff/Admin
3. **Export Excel**: Xuất danh sách nhân viên
4. **Audit logs**: Lưu lịch sử thay đổi
5. **Soft delete**: Xóa mềm thay vì xóa cứng
6. **Email notification**: Gửi email khi reset password

---

## 📝 Notes

- Tất cả password đều được hash bằng **BCrypt** trước khi lưu DB
- JWT token có thời hạn, cần refresh khi hết hạn
- Admin không thể tự khóa tài khoản của mính h
- Validation ở cả frontend và backend (defense in depth)