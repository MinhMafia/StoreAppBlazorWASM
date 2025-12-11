using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
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
                throw new InvalidOperationException(error ?? "Mật khẩu hiện tại không đúng hoặc dữ liệu không hợp lệ.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(error ?? "Không tìm thấy người dùng.");
            }

            throw new InvalidOperationException(error ?? "Đổi mật khẩu thất bại.");
        }

        private async Task AttachUserIdHeaderAsync(HttpRequestMessage request)
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            if (string.IsNullOrWhiteSpace(token))
                return;

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

                if (doc.RootElement.TryGetProperty("uid", out var uidProp))
                {
                    return uidProp.GetString();
                }

                if (doc.RootElement.TryGetProperty("userId", out var userIdProp))
                {
                    return userIdProp.GetString();
                }
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
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString();
                }

                if (doc.RootElement.ValueKind == JsonValueKind.String)
                {
                    return doc.RootElement.GetString();
                }
            }
            catch
            {
                // Ignore parsing errors and fall back to null.
            }

            return null;
        }
    }
}
