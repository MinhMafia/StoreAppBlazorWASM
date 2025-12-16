using System.Net.Http.Json;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
    public interface IUserClientService
    {
        Task<PaginationResult<UserDTO>> GetStaffsAsync(int page = 1, int pageSize = 10);
        Task<List<UserDTO>> GetAllUsersAsync();
        Task<UserDTO?> GetUserByIdAsync(int id);
        Task<UserDTO?> CreateUserAsync(UserDTO user);
        Task<UserDTO?> UpdateUserAsync(int id, UserDTO user);
        Task<bool> ToggleUserStatusAsync(int id, bool isActive);
        Task<bool> ResetPasswordAsync(int id, string newPassword);
        Task<bool> DeleteUserAsync(int id);
    }

    public class UserClientService : IUserClientService
    {
        private readonly HttpClient _http;

        public UserClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PaginationResult<UserDTO>> GetStaffsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var response = await _http.GetFromJsonAsync<PaginationResult<UserDTO>>(
                    $"api/users/staffs?page={page}&pageSize={pageSize}");
                return response ?? new PaginationResult<UserDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading staffs: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<List<UserDTO>>("api/users/getalluser");
                return response ?? new List<UserDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
                throw;
            }
        }

        public async Task<UserDTO?> GetUserByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<UserDTO>($"api/users/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<UserDTO?> CreateUserAsync(UserDTO user)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/users", user);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }

        public async Task<UserDTO?> UpdateUserAsync(int id, UserDTO user)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/users/{id}", user);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int id, bool isActive)
        {
            try
            {
                var response = await _http.PatchAsync($"api/users/{id}/status?isActive={isActive}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling user status {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            try
            {
                // Get current user data first
                var currentUser = await GetUserByIdAsync(id);
                if (currentUser == null)
                {
                    Console.WriteLine($"User {id} not found");
                    return false;
                }

                // Update with new password
                currentUser.Password = newPassword;
                var response = await _http.PutAsJsonAsync($"api/users/{id}", currentUser);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password for user {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/users/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user {id}: {ex.Message}");
                return false;
            }
        }
    }
}
