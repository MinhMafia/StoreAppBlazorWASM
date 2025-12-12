using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace StoreApp.Client.Middlewares
{
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _nav;

        public JwtAuthorizationMessageHandler(ILocalStorageService localStorage, NavigationManager nav)
        {
            _localStorage = localStorage;
            _nav = nav;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 1. Attach token nếu có
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            var hasToken = !string.IsNullOrWhiteSpace(token);

            if (hasToken)
            {
                var cleanToken = token.Trim('"');
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 2. Auto logout khi có token và nhận 401 (hết hạn/sai). Bỏ qua các auth endpoint.
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var isAuthEndpoint = path.Contains("/api/auth", StringComparison.OrdinalIgnoreCase);

            if (hasToken && !isAuthEndpoint && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("userName");
                await _localStorage.RemoveItemAsync("userRole");

                if (!_nav.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
                {
                    _nav.NavigateTo("/login", forceLoad: true);
                }
            }

            return response;
        }
    }
}
