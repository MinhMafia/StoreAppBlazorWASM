// Services/CustomerAuthService.cs
using Blazored.LocalStorage;
using System.Net.Http.Json;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
    public interface ICustomerAuthService
    {
        Task SetCustomerIdAsync(int customerId);
        Task<int?> GetCustomerIdAsync();
        Task<CustomerResponseDTO?> GetCurrentCustomerAsync();
        Task ClearCustomerAsync();
        Task<bool> IsAuthenticatedAsync();
    }

    public class CustomerAuthService : ICustomerAuthService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;
        private const string CUSTOMER_ID_KEY = "customerId";
        private const string AUTH_TOKEN_KEY = "authToken";

        public CustomerAuthService(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync(AUTH_TOKEN_KEY);
                return !string.IsNullOrEmpty(token?.Trim('"'));
            }
            catch
            {
                return false;
            }
        }

        public async Task SetCustomerIdAsync(int customerId)
        {
            await _localStorage.SetItemAsync(CUSTOMER_ID_KEY, customerId);
        }

        public async Task<int?> GetCustomerIdAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<int?>(CUSTOMER_ID_KEY);
            }
            catch
            {
                return null;
            }
        }

        public async Task<CustomerResponseDTO?> GetCurrentCustomerAsync()
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue)
                return null;

            try
            {
                return await _httpClient.GetFromJsonAsync<CustomerResponseDTO>($"api/customers/{customerId.Value}");
            }
            catch
            {
                return null;
            }
        }

        public async Task ClearCustomerAsync()
        {
            await _localStorage.RemoveItemAsync(CUSTOMER_ID_KEY);
        }
    }
}

