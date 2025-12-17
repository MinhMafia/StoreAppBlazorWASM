# Chức năng Quản lý Thông tin Cá nhân

## 📋 Tổng quan

Hệ thống quản lý thông tin cá nhân cho phép **Users (Admin/Staff)** và **Customers** xem, chỉnh sửa thông tin cá nhân và đổi mật khẩu. Hệ thống hỗ trợ 2 loại tài khoản với các trường thông tin khác nhau:

- **Users (Admin/Staff)**: Username, Email, Họ tên
- **Customers**: Email (username), Họ tên, Số điện thoại, Địa chỉ

---

## 🎯 Các chức năng chính

### 1. **Xem thông tin cá nhân**
- Hiển thị thông tin user/customer hiện tại
- Phân biệt role (admin/staff/customer) để hiển thị đúng trường
- Load từ JWT token (tự động nhận diện user ID)

### 2. **Chỉnh sửa thông tin**
- **Users**: Sửa Username, Email, Họ tên
- **Customers**: Sửa Họ tên, Email, Số điện thoại, Địa chỉ
- Validation realtime (email format, phone format, độ dài)
- Username và Email (customer) là read-only

### 3. **Đổi mật khẩu**
- Yêu cầu nhập mật khẩu hiện tại
- Nhập mật khẩu mới + xác nhận
- Toggle hiển thị/ẩn mật khẩu
- Validation: min 6 ký tự, khớp nhau

---

## 🏗️ Kiến trúc

### Frontend (Blazor WebAssembly)

#### **1. Page Component**
```razor
// filepath: StoreApp.Client/Pages/Store/StoreProfile.razor
@page "/store/profile"
@layout StoreLayout
@inject IMeClientService MeClientService

<PageTitle>Hồ sơ cá nhân</PageTitle>

<div class="max-w-4xl mx-auto px-4 py-10">
    <div class="flex items-start justify-between">
        <div>
            <h1 class="text-3xl font-bold">Hồ sơ cá nhân</h1>
            <p class="text-sm text-gray-500">Xem thông tin và cập nhật tên hiển thị.</p>
        </div>
        <button @onclick="OpenPasswordModal">Đổi mật khẩu</button>
    </div>

    <!-- Alert -->
    @if (alert is not null)
    {
        <div class="@GetAlertClass(alert)">@alert.Message</div>
    }

    <!-- Profile Form -->
    <div class="bg-white rounded-2xl shadow p-6">
        <EditForm Model="@profileForm" OnSubmit="@HandleProfileSubmit">
            <div class="grid grid-cols-2 gap-4">
                <!-- Username (Read-only) -->
                <div>
                    <label>Username</label>
                    <input value="@profileForm.Username" readonly />
                </div>

                <!-- Email (Read-only) -->
                <div>
                    <label>Email</label>
                    <input value="@profileForm.Email" readonly />
                </div>
            </div>

            <!-- Full Name (Editable) -->
            <div>
                <label>Họ tên</label>
                <input @bind="profileForm.FullName" placeholder="Nhập họ tên" />
            </div>

            <!-- Phone & Address (Customers only) -->
            <div class="grid grid-cols-2 gap-4">
                <div>
                    <label>Số điện thoại</label>
                    <input @bind="profileForm.Phone" placeholder="0912345678" />
                </div>
                <div>
                    <label>Địa chỉ</label>
                    <input @bind="profileForm.Address" placeholder="Địa chỉ nhận hàng" />
                </div>
            </div>

            <div class="flex justify-end gap-3">
                <button type="button" @onclick="LoadProfileAsync">Tải lại</button>
                <button type="submit" disabled="@isSaving">
                    @(isSaving ? "Đang lưu..." : "Lưu thay đổi")
                </button>
            </div>
        </EditForm>
    </div>
</div>

<!-- Change Password Modal -->
@if (showPasswordModal)
{
    <div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
        <div class="bg-white max-w-lg rounded-2xl p-6">
            <h2 class="text-xl font-semibold">Đổi mật khẩu</h2>

            @if (passwordAlert is not null)
            {
                <div class="@GetAlertClass(passwordAlert)">@passwordAlert.Message</div>
            }

            <EditForm Model="@passwordForm" OnSubmit="@HandlePasswordSubmit">
                <!-- Current Password -->
                <div>
                    <label>Mật khẩu hiện tại</label>
                    <div class="relative">
                        <input type="@(showCurrentPassword ? "text" : "password")"
                               @bind="passwordForm.CurrentPassword" required />
                        <button type="button" 
                                @onclick="() => showCurrentPassword = !showCurrentPassword">
                            <i class="bi @(showCurrentPassword ? "bi-eye-slash" : "bi-eye")"></i>
                        </button>
                    </div>
                </div>

                <!-- New Password -->
                <div>
                    <label>Mật khẩu mới</label>
                    <input type="@(showNewPassword ? "text" : "password")"
                           @bind="passwordForm.NewPassword" 
                           minlength="6" required />
                </div>

                <!-- Confirm Password -->
                <div>
                    <label>Xác nhận mật khẩu mới</label>
                    <input type="@(showConfirmPassword ? "text" : "password")"
                           @bind="passwordForm.ConfirmNewPassword" 
                           minlength="6" required />
                </div>

                <div class="flex justify-end gap-3">
                    <button type="button" @onclick="ClosePasswordModal">Hủy</button>
                    <button type="submit" disabled="@isPasswordSaving">
                        @(isPasswordSaving ? "Đang lưu..." : "Đổi mật khẩu")
                    </button>
                </div>
            </EditForm>
        </div>
    </div>
}

@code {
    private ProfileForm profileForm = new();
    private PasswordForm passwordForm = new();
    private AlertMessage? alert;
    private AlertMessage? passwordAlert;
    private bool isLoading = true;
    private bool isSaving;
    private bool isPasswordSaving;
    private bool showPasswordModal;
    private bool showCurrentPassword;
    private bool showNewPassword;
    private bool showConfirmPassword;

    protected override async Task OnInitializedAsync()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        isLoading = true;
        try
        {
            var data = await MeClientService.GetProfileAsync();
            profileForm = new ProfileForm
            {
                Username = data.Username ?? string.Empty,
                Email = data.Email ?? string.Empty,
                FullName = data.FullName ?? string.Empty,
                Phone = data.Phone ?? string.Empty,
                Address = data.Address ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            alert = AlertMessage.Error(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleProfileSubmit(EditContext _)
    {
        // Validation
        var trimmedFullName = profileForm.FullName?.Trim() ?? string.Empty;
        if (trimmedFullName.Length < 3)
        {
            alert = AlertMessage.Error("Họ tên phải có ít nhất 3 ký tự.");
            return;
        }

        // Phone validation (if exists)
        var phoneRegex = new Regex(@"^(0|\+84)[35789]\d{8}$");
        if (!string.IsNullOrWhiteSpace(profileForm.Phone) && 
            !phoneRegex.IsMatch(profileForm.Phone))
        {
            alert = AlertMessage.Error("Số điện thoại không hợp lệ.");
            return;
        }

        isSaving = true;
        try
        {
            var payload = new MeDTO
            {
                Username = profileForm.Username,
                Email = profileForm.Email,
                FullName = trimmedFullName,
                Phone = profileForm.Phone,
                Address = profileForm.Address
            };

            var updated = await MeClientService.UpdateProfileAsync(payload);
            profileForm.FullName = updated.FullName;
            alert = AlertMessage.Success("Thông tin đã được cập nhật.");
        }
        catch (Exception ex)
        {
            alert = AlertMessage.Error(ex.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandlePasswordSubmit(EditContext _)
    {
        if (passwordForm.NewPassword != passwordForm.ConfirmNewPassword)
        {
            passwordAlert = AlertMessage.Error("Mật khẩu xác nhận không khớp.");
            return;
        }

        isPasswordSaving = true;
        try
        {
            var request = new MeDTO
            {
                CurrentPassword = passwordForm.CurrentPassword,
                NewPassword = passwordForm.NewPassword,
                ConfirmNewPassword = passwordForm.ConfirmNewPassword
            };

            await MeClientService.ChangePasswordAsync(request);
            passwordForm = new PasswordForm();
            showPasswordModal = false;
            alert = AlertMessage.Success("Mật khẩu đã được thay đổi.");
        }
        catch (Exception ex)
        {
            passwordAlert = AlertMessage.Error(ex.Message);
        }
        finally
        {
            isPasswordSaving = false;
        }
    }

    private void OpenPasswordModal()
    {
        passwordAlert = null;
        passwordForm = new PasswordForm();
        showPasswordModal = true;
    }

    private void ClosePasswordModal()
    {
        showPasswordModal = false;
    }

    // Form models
    private sealed class ProfileForm
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    private sealed class PasswordForm
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    private sealed record AlertMessage(string Type, string Message)
    {
        public static AlertMessage Success(string msg) => new("success", msg);
        public static AlertMessage Error(string msg) => new("error", msg);
    }
}
```

#### **2. Service Client**
```csharp
// filepath: StoreApp.Client/Services/MeClientService.cs
public interface IMeClientService
{
    Task<MeDTO?> GetProfileAsync();
    Task<MeDTO> UpdateProfileAsync(MeDTO request);
    Task ChangePasswordAsync(MeDTO request);
}

public class MeClientService : IMeClientService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public MeClientService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<MeDTO?> GetProfileAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/me");
            await AttachUserIdHeaderAsync(request);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MeDTO>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<MeDTO> UpdateProfileAsync(MeDTO request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, "api/me")
        {
            Content = JsonContent.Create(request)
        };

        await AttachUserIdHeaderAsync(httpRequest);
        var response = await _http.SendAsync(httpRequest);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MeDTO>() ?? new MeDTO();
        }

        var error = await ReadErrorMessageAsync(response);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(error ?? "Username hoặc email đã tồn tại.");
        }

        throw new InvalidOperationException(error ?? "Cập nhật hồ sơ thất bại.");
    }

    public async Task ChangePasswordAsync(MeDTO request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, "api/me/change-password")
        {
            Content = JsonContent.Create(request)
        };

        await AttachUserIdHeaderAsync(httpRequest);
        var response = await _http.SendAsync(httpRequest);

        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK)
        {
            return;
        }

        var error = await ReadErrorMessageAsync(response);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                error ?? "Mật khẩu hiện tại không đúng hoặc dữ liệu không hợp lệ.");
        }

        throw new InvalidOperationException(error ?? "Đổi mật khẩu thất bại.");
    }

    private async Task AttachUserIdHeaderAsync(HttpRequestMessage request)
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrWhiteSpace(token)) return;

        var userId = ExtractUserId(token.Trim('"'));
        if (!string.IsNullOrWhiteSpace(userId) && !request.Headers.Contains("X-User-Id"))
        {
            request.Headers.Add("X-User-Id", userId);
        }
    }

    private static string? ExtractUserId(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);

            // Staff/Admin token uses "uid"
            if (doc.RootElement.TryGetProperty("uid", out var uidProp))
                return uidProp.GetString();

            // Customer token uses "customerId"
            if (doc.RootElement.TryGetProperty("customerId", out var customerIdProp))
                return customerIdProp.GetString();

            // Fallback to nameid
            if (doc.RootElement.TryGetProperty("nameid", out var nameIdProp))
                return nameIdProp.GetString();
        }
        catch
        {
            // Ignore malformed token
        }

        return null;
    }

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                return messageProp.GetString();
            }
        }
        catch { }
        return null;
    }
}
```

#### **3. Đăng ký Service**
```csharp
// filepath: StoreApp.Client/Program.cs
// ...existing code...

builder.Services.AddHttpClient<IMeClientService, MeClientService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

// ...existing code...
```

---

### Backend (ASP.NET Core Web API)

#### **1. Controller**
```csharp
// filepath: StoreApp/Controllers/MeController.cs
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CustomerService _customerService;

    public MeController(UserService userService, CustomerService customerService)
    {
        _userService = userService;
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult> GetProfile()
    {
        var userId = ResolveUserId();
        if (userId == null)
            return Unauthorized("Missing X-User-Id header.");

        var role = User?.FindFirst(ClaimTypes.Role)?.Value;

        // Customer profile
        if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
        {
            var customerResult = await _customerService.GetCustomerByIdAsync(userId.Value);
            if (customerResult?.Data == null)
                return NotFound("Customer not found.");

            return Ok(MapToMeDto(customerResult.Data));
        }

        // Staff/Admin profile
        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null)
            return NotFound("User not found.");

        return Ok(MapToMeDto(user));
    }

    [HttpPut]
    public async Task<ActionResult> UpdateProfile([FromBody] MeDTO request)
    {
        var userId = ResolveUserId();
        if (userId == null)
            return Unauthorized("Missing X-User-Id header.");

        var role = User?.FindFirst(ClaimTypes.Role)?.Value;

        // Customer update
        if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
        {
            ValidateCustomerProfilePayload(request);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var updateDto = new CustomerUpdateDTO
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };

            var result = await _customerService.UpdateCustomerAsync(userId.Value, updateDto);
            if (result?.Data == null)
                return NotFound("Customer not found.");

            return Ok(MapToMeDto(result.Data));
        }

        // Staff/Admin update
        ValidateProfilePayload(request);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _userService.UserExistsUsernameAsync(request.Username, userId.Value))
            return Conflict("Username already exists.");

        if (await _userService.UserExistsEmailAsync(request.Email, userId.Value))
            return Conflict("Email already exists.");

        var updated = await _userService.UpdateProfileAsync(userId.Value, request);
        if (updated == null)
            return NotFound("User not found.");

        return Ok(MapToMeDto(updated));
    }

    [HttpPut("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] MeDTO request)
    {
        var userId = ResolveUserId();
        if (userId == null)
            return Unauthorized("Missing X-User-Id header.");

        // Ignore profile fields
        ModelState.Remove(nameof(MeDTO.Username));
        ModelState.Remove(nameof(MeDTO.Email));

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            ModelState.AddModelError(nameof(MeDTO.CurrentPassword), 
                "Current password is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            ModelState.AddModelError(nameof(MeDTO.NewPassword), 
                "New password is required.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _userService.ChangePasswordAsync(
            userId.Value,
            request.CurrentPassword!,
            request.NewPassword!
        );

        if (result == UserService.ChangePasswordResult.UserNotFound)
            return NotFound("User not found.");

        if (result == UserService.ChangePasswordResult.InvalidCurrentPassword)
            return BadRequest("Current password is incorrect.");

        return NoContent();
    }

    private int? ResolveUserId()
    {
        // Ưu tiên lấy từ JWT claim
        var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(idClaim) && int.TryParse(idClaim, out var claimUserId))
        {
            return claimUserId;
        }

        // Fallback: X-User-Id header
        if (Request.Headers.TryGetValue("X-User-Id", out var header) &&
            int.TryParse(header, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static MeDTO MapToMeDto(UserDTO user)
    {
        return new MeDTO
        {
            Username = user.Username ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        };
    }

    private static MeDTO MapToMeDto(CustomerResponseDTO customer)
    {
        return new MeDTO
        {
            Username = customer.Email ?? string.Empty,
            Email = customer.Email ?? string.Empty,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Address = customer.Address
        };
    }

    private void ValidateProfilePayload(MeDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            ModelState.AddModelError(nameof(MeDTO.Username), "Username is required.");
        else if (request.Username.Length < 3)
            ModelState.AddModelError(nameof(MeDTO.Username), 
                "Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(request.Email))
            ModelState.AddModelError(nameof(MeDTO.Email), "Email is required.");
        else if (!new EmailAddressAttribute().IsValid(request.Email))
            ModelState.AddModelError(nameof(MeDTO.Email), "Email is invalid.");
    }

    private void ValidateCustomerProfilePayload(MeDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            ModelState.AddModelError(nameof(MeDTO.FullName), "Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            ModelState.AddModelError(nameof(MeDTO.Email), "Email is required.");

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phoneRegex = new Regex(@"^(0|\+84)[35789]\d{8}$");
            if (!phoneRegex.IsMatch(request.Phone))
                ModelState.AddModelError(nameof(MeDTO.Phone), 
                    "Phone number is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(request.Address) && request.Address.Length > 250)
            ModelState.AddModelError(nameof(MeDTO.Address), 
                "Address must be at most 250 characters.");
    }
}
```

#### **2. Service Layer**
```csharp
// filepath: StoreApp/Services/UserService.cs
public async Task<UserDTO?> UpdateProfileAsync(int id, MeDTO request)
{
    var existingUser = await _userRepository.GetByIdAsync(id);
    if (existingUser == null) return null;

    existingUser.Username = request.Username.Trim();
    existingUser.Email = request.Email.Trim();
    existingUser.FullName = string.IsNullOrWhiteSpace(request.FullName) 
        ? null 
        : request.FullName.Trim();
    existingUser.UpdatedAt = DateTime.UtcNow;

    var updatedUser = await _userRepository.UpdateAsync(existingUser);
    return MapToUserDto(updatedUser);
}

public async Task<ChangePasswordResult> ChangePasswordAsync(
    int id, string currentPassword, string newPassword)
{
    var existingUser = await _userRepository.GetByIdAsync(id);
    if (existingUser == null) 
        return ChangePasswordResult.UserNotFound;

    if (!VerifyPassword(existingUser.PasswordHash, currentPassword))
        return ChangePasswordResult.InvalidCurrentPassword;

    existingUser.PasswordHash = HashPassword(newPassword);
    existingUser.UpdatedAt = DateTime.UtcNow;

    await _userRepository.UpdateAsync(existingUser);
    return ChangePasswordResult.Success;
}

public enum ChangePasswordResult
{
    Success,
    UserNotFound,
    InvalidCurrentPassword
}
```

#### **3. DTOs**
```csharp
// filepath: StoreApp.Shared/DTO/MeDTO.cs
public class MeDTO
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Password change fields
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}
```

---

## 🔐 Bảo mật

### **1. JWT Token Extraction**
Service tự động extract user ID từ JWT token:
```csharp
private static string? ExtractUserId(string token)
{
    var parts = token.Split('.');
    var payload = parts[1]; // JWT payload
    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    using var doc = JsonDocument.Parse(json);

    // Staff/Admin: claim "uid"
    if (doc.RootElement.TryGetProperty("uid", out var uidProp))
        return uidProp.GetString();

    // Customer: claim "customerId"
    if (doc.RootElement.TryGetProperty("customerId", out var customerIdProp))
        return customerIdProp.GetString();

    return null;
}
```

### **2. X-User-Id Header**
Tự động gắn header cho mọi request:
```csharp
private async Task AttachUserIdHeaderAsync(HttpRequestMessage request)
{
    var token = await _localStorage.GetItemAsStringAsync("authToken");
    var userId = ExtractUserId(token);
    
    if (!string.IsNullOrWhiteSpace(userId))
    {
        request.Headers.Add("X-User-Id", userId);
    }
}
```

### **3. Authorization**
- Controller yêu cầu `[Authorize]` - phải đăng nhập
- Backend phân biệt User/Customer bằng `ClaimTypes.Role`
- Password change yêu cầu mật khẩu hiện tại

---

## 📊 Flow hoạt động

### **1. Load profile**
```
User visits /store/profile
  ↓
OnInitializedAsync() → LoadProfileAsync()
  ↓
IMeClientService.GetProfileAsync()
  ↓
HTTP GET /api/me
  + Header: Authorization: Bearer <JWT_TOKEN>
  + Header: X-User-Id: <extracted_from_token>
  ↓
MeController.GetProfile()
  → ResolveUserId() from claims/header
  → Check role (customer vs staff/admin)
  → UserService.GetUserByIdAsync() OR CustomerService.GetCustomerByIdAsync()
  ↓
Return MeDTO
  ↓
Populate ProfileForm
  ↓
Display in UI
```

### **2. Update profile**
```
User edits form → SubmitProfileForm()
  ↓
Validation (client-side)
  - FullName min 3 chars
  - Phone format: ^(0|\+84)[35789]\d{8}$
  - Address max 250 chars
  ↓
IMeClientService.UpdateProfileAsync(MeDTO)
  ↓
HTTP PUT /api/me
  + Body: { Username, Email, FullName, Phone, Address }
  + Header: Authorization + X-User-Id
  ↓
MeController.UpdateProfile()
  → ValidateProfilePayload() or ValidateCustomerProfilePayload()
  → Check role
  → UserService.UpdateProfileAsync() OR CustomerService.UpdateCustomerAsync()
  → Update database
  ↓
Return updated MeDTO
  ↓
Show success alert
```

### **3. Change password**
```
User clicks "Đổi mật khẩu" → OpenPasswordModal()
  ↓
User fills form:
  - CurrentPassword
  - NewPassword
  - ConfirmNewPassword
  ↓
SubmitPasswordForm()
  ↓
Validation (client-side)
  - NewPassword === ConfirmNewPassword
  - NewPassword.Length >= 6
  ↓
IMeClientService.ChangePasswordAsync(MeDTO)
  ↓
HTTP PUT /api/me/change-password
  + Body: { CurrentPassword, NewPassword, ConfirmNewPassword }
  ↓
MeController.ChangePassword()
  → ResolveUserId()
  → UserService.ChangePasswordAsync(id, current, new)
    → Verify current password (BCrypt.Verify)
    → Hash new password (BCrypt.HashPassword)
    → Update database
  ↓
Return 204 NoContent
  ↓
Close modal + Show success alert
```

---

## 🎨 UI/UX Features

### **1. Read-only Fields**
- **Username** và **Email** là read-only (không cho sửa)
- Hiển thị với `bg-gray-50` và `readonly` attribute

### **2. Toggle Password Visibility**
```razor
<input type="@(showCurrentPassword ? "text" : "password")"
       @bind="passwordForm.CurrentPassword" />
<button @onclick="() => showCurrentPassword = !showCurrentPassword">
    <i class="bi @(showCurrentPassword ? "bi-eye-slash" : "bi-eye")"></i>
</button>
```

### **3. Alert Separation**
- **Page-level alert**: Hiển thị kết quả update profile
- **Modal alert**: Hiển thị lỗi trong modal đổi password
- Không bị conflict vì dùng biến riêng (`alert` vs `passwordAlert`)

### **4. Loading States**
- `isLoading`: Loading khi fetch profile
- `isSaving`: Đang lưu profile
- `isPasswordSaving`: Đang đổi mật khẩu
- Disable buttons và hiển thị "Đang lưu..."

---

## 🔄 Phân biệt User vs Customer

| Feature | User (Admin/Staff) | Customer |
|---------|-------------------|----------|
| **Username** | Có, read-only | Không (dùng Email) |
| **Email** | Có, editable | Có, read-only |
| **FullName** | Có, editable | Có, editable |
| **Phone** | Không | Có, editable |
| **Address** | Không | Có, editable |
| **JWT Claim** | `"uid"` | `"customerId"` |
| **Backend Service** | `UserService` | `CustomerService` |
| **Validation** | `ValidateProfilePayload` | `ValidateCustomerProfilePayload` |

---

## ✅ Các vấn đề đã giải quyết

### **1. Extract User ID từ JWT**
- **Vấn đề**: Backend cần biết user nào đang request
- **Giải pháp**: Frontend decode JWT, extract `uid` hoặc `customerId`, gắn vào header `X-User-Id`

### **2. Phân biệt User và Customer**
- **Vấn đề**: 2 loại tài khoản có cấu trúc khác nhau
- **Giải pháp**: Backend check `ClaimTypes.Role` để gọi đúng service

### **3. Password change validation**
- **Vấn đề**: Cần verify mật khẩu cũ
- **Giải pháp**: Backend dùng `BCrypt.Verify()` để check password hiện tại

### **4. Conflict khi update**
- **Vấn đề**: Username/Email có thể trùng
- **Giải pháp**: Check exists trước khi update, trả về 409 Conflict

### **5. Modal state management**
- **Vấn đề**: Alert trong modal bị conflict với page alert
- **Giải pháp**: Tách biệt `alert` và `passwordAlert`

---

## 🚀 Cải tiến trong tương lai

1. **Avatar upload**: Cho phép user upload ảnh đại diện
2. **Email verification**: Xác thực email khi thay đổi
3. **Password strength meter**: Hiển thị độ mạnh mật khẩu
4. **Activity log**: Lịch sử thay đổi thông tin
5. **Two-factor authentication**: Xác thực 2 lớp
6. **Social login**: Đăng nhập bằng Google/Facebook

---

## 📝 Notes

- Password được hash bằng **BCrypt** với work factor mặc định (10)
- JWT token được lưu trong **LocalStorage** với key `"authToken"`
- Token tự động gắn vào header bởi `JwtAuthorizationMessageHandler`
- Validation ở cả **frontend** (UX) và **backend** (security)
- Username và Email của User có thể trùng với Customer (khác table)
- Phone regex: `^(0|\+84)[35789]\d{8}$` (VN format only)
- Address max length: 250 ký tự
- Password min length: 6 ký tự

---

## 🔗 Related Documentation

- [Quản lý Nhân viên](./QUAN_LY_NHAN_VIEN.md)
- [JWT Authentication](./JWT_AUTHENTICATION.md)
- [Customer Management](./CUSTOMER_MANAGEMENT.md)
