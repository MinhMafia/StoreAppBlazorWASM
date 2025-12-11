using StoreApp.Shared;
using StoreApp.Models;
using StoreApp.Repository;
using System.Security.Cryptography;
using System.Text;

namespace StoreApp.Services
{
    public class UserService
    {
        public enum ChangePasswordResult
        {
            Success,
            UserNotFound,
            InvalidCurrentPassword
        }

        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<PaginationResult<UserDTO>> GetPaginatedAsync(int page, int pageSize)
        {
            var result = await _userRepository.GetPaginatedAsync(page, pageSize);

            return new PaginationResult<UserDTO>
            {
                Items = result.Items.Select(MapToUserDto).ToList(),
                TotalItems = result.TotalItems,
                CurrentPage = result.CurrentPage,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasPrevious = result.HasPrevious,
                HasNext = result.HasNext
            };
        }

        public async Task<PaginationResult<UserDTO>> GetNonAdminPaginatedAsync(int page, int pageSize)
        {
            var result = await _userRepository.GetNonAdminPaginatedAsync(page, pageSize);

            return new PaginationResult<UserDTO>
            {
                Items = result.Items.Select(MapToUserDto).ToList(),
                TotalItems = result.TotalItems,
                CurrentPage = result.CurrentPage,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasPrevious = result.HasPrevious,
                HasNext = result.HasNext
            };
        }

        public async Task<UserDTO?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToUserDto(user);
        }

        public async Task<UserDTO?> CreateUserAsync(UserDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required", nameof(request.Password));

            var now = DateTime.UtcNow;

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
                Role = NormalizeRole(request.Role),
                IsActive = request.IsActive,
                IsLocked = !request.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordHash = HashPassword(request.Password)
            };

            var createdUser = await _userRepository.AddAsync(user);
            return MapToUserDto(createdUser);
        }

        public async Task<UserDTO?> UpdateUserAsync(int id, UserDTO request)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null) return null;

            existingUser.Username = request.Username.Trim();
            existingUser.Email = request.Email.Trim();
            existingUser.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
            existingUser.Role = NormalizeRole(request.Role);
            existingUser.IsActive = request.IsActive;
            existingUser.IsLocked = !request.IsActive;
            existingUser.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                existingUser.PasswordHash = HashPassword(request.Password);
            }

            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            return MapToUserDto(updatedUser);
        }

        public async Task<UserDTO?> UpdateProfileAsync(int id, MeDTO request)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null) return null;

            existingUser.Username = request.Username.Trim();
            existingUser.Email = request.Email.Trim();
            existingUser.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
            existingUser.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            return MapToUserDto(updatedUser);
        }

        public async Task<UserDTO?> UpdateStatusAsync(int id, bool isActive)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null) return null;

            existingUser.IsActive = isActive;
            existingUser.IsLocked = !isActive;
            existingUser.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            return MapToUserDto(updatedUser);
        }

        public async Task<ChangePasswordResult> ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null) return ChangePasswordResult.UserNotFound;

            if (!VerifyPassword(existingUser.PasswordHash, currentPassword))
                return ChangePasswordResult.InvalidCurrentPassword;

            existingUser.PasswordHash = HashPassword(newPassword);
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existingUser);
            return ChangePasswordResult.Success;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> UserExistsEmailAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var user = await _userRepository.GetByEmailAsync(email.Trim());
            if (user == null) return false;

            return excludeUserId == null || user.Id != excludeUserId.Value;
        }

        public async Task<bool> UserExistsUsernameAsync(string username, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            var user = await _userRepository.GetByUsernameAsync(username.Trim());
            if (user == null) return false;

            return excludeUserId == null || user.Id != excludeUserId.Value;
        }

        private UserDTO MapToUserDto(User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLogin
            };
        }

        private static string NormalizeRole(string role)
        {
            var normalized = string.IsNullOrWhiteSpace(role)
                ? "staff"
                : role.Trim().ToLowerInvariant();

            return normalized == "admin" ? "admin" : "staff";
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static string HashPasswordLegacy(string password)
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static bool VerifyPassword(string hashedPassword, string password)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return false;

            // BCrypt hash starts with $2...
            if (hashedPassword.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }

            // Legacy SHA256 fallback
            return string.Equals(hashedPassword, HashPasswordLegacy(password), StringComparison.OrdinalIgnoreCase);
        }
    }
}
