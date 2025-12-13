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
    /// <summary>
    /// Attaches JWT to outgoing requests and auto-logs out on 401 (except auth/cart endpoints).
    /// </summary>
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _nav;

        public JwtAuthorizationMessageHandler(ILocalStorageService localStorage, NavigationManager nav)
        {
            _localStorage = localStorage;
            _nav = nav;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            var hasToken = !string.IsNullOrWhiteSpace(token);

            if (hasToken)
            {
                // LocalStorage string may include quotes, trim them.
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim('"'));
            }

            var response = await base.SendAsync(request, cancellationToken);

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var isAuthEndpoint = path.Contains("/api/auth", StringComparison.OrdinalIgnoreCase);
            // Do not force logout when cart endpoint returns 401 (e.g., role mismatch)
            var ignoreAutoLogout = path.StartsWith("/api/cart", StringComparison.OrdinalIgnoreCase);

            if (hasToken && !isAuthEndpoint && !ignoreAutoLogout && response.StatusCode == HttpStatusCode.Unauthorized)
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
