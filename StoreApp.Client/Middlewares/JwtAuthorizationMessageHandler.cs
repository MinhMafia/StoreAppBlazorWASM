using Blazored.LocalStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace StoreApp.Client.Middlewares
{
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public JwtAuthorizationMessageHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // 1. Lấy token từ LocalStorage
            var token = await _localStorage.GetItemAsStringAsync("authToken"); // Thay "authToken" bằng key bạn lưu token

            // 2. Nếu có token, thêm nó vào header Authorization
            if (!string.IsNullOrEmpty(token))
            {
                // Loại bỏ dấu ngoặc kép nếu token được lưu dưới dạng string JSON
                string cleanToken = token.Trim('"'); 
                
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanToken);
            }

            // 3. Tiếp tục gửi request đến API
            return await base.SendAsync(request, cancellationToken);
        }
    }
}