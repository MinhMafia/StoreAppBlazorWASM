// Services/AuthService.cs
using Blazored.LocalStorage;
using System.Net.Http.Json;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
    public interface IAuthService
    {
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetUserRoleAsync();
        Task<string?> GetUserNameAsync();
        Task LogoutAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly ILocalStorageService _localStorage;

        public AuthService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync("authToken");
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetUserRoleAsync()
        {
            try
            {
                return await _localStorage.GetItemAsStringAsync("userRole");
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUserNameAsync()
        {
            try
            {
                return await _localStorage.GetItemAsStringAsync("userName");
            }
            catch
            {
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userName");
            await _localStorage.RemoveItemAsync("userRole");
        }
    }
}