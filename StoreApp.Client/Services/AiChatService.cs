using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Blazored.LocalStorage;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
   
    public class AiChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private const string API_BASE = "api/ai";

        public AiChatService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task<string?> GetAuthTokenAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync("authToken");
                return token?.Trim('"');
            }
            catch
            {
                return null;
            }
        }

        public async Task StreamMessageAsync(
            string message,
            int? conversationId,
            List<ClientMessageDTO>? history,
            Func<string, Task> onChunk,
            Func<int, Task> onConversationId,
            Func<string, Task> onError,
            Func<Task> onComplete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    await onError("Vui lòng đăng nhập để sử dụng AI Chat");
                    return;
                }

                var request = new AiChatRequestDTO
                {
                    Message = message,
                    ConversationId = conversationId,
                    History = history
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE}/stream")
                {
                    Content = content
                };

                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

               
                httpRequest.SetBrowserResponseStreamingEnabled(true);

                // Send request va doc stream - ResponseHeadersRead de bat dau doc ngay khi co headers
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await onError($"HTTP {response.StatusCode}: {error}");
                    return;
                }

                
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                
                
                var buffer = new StringBuilder();
                var readBuffer = new byte[4096];
                var decoder = Encoding.UTF8;
                
                
                var contentBuffer = new StringBuilder();
                var lastUpdateTime = DateTime.UtcNow;
                const int UPDATE_INTERVAL_MS = 50; 

                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                    if (bytesRead == 0) break; // End of stream

                    
                    buffer.Append(decoder.GetString(readBuffer, 0, bytesRead));

                   
                    var bufferStr = buffer.ToString();
                    var events = bufferStr.Split(new[] { "\n\n" }, StringSplitOptions.None);
                    
                   
                    buffer.Clear();
                    if (events.Length > 0)
                    {
                        buffer.Append(events[^1]); 
                    }

                   
                    for (int i = 0; i < events.Length - 1; i++)
                    {
                        var eventData = events[i].Trim();
                        if (string.IsNullOrEmpty(eventData)) continue;

                        
                        foreach (var line in eventData.Split('\n'))
                        {
                            if (!line.StartsWith("data: ")) continue;

                            var data = line.Substring(6);

                            if (data == "[DONE]")
                            {
                                // Flush remaining content trước khi complete
                                if (contentBuffer.Length > 0)
                                {
                                    await onChunk(contentBuffer.ToString());
                                    contentBuffer.Clear();
                                }
                                await onComplete();
                                return;
                            }

                            try
                            {
                                var chunk = JsonSerializer.Deserialize<StreamChunkDTO>(data);
                                if (chunk != null)
                                {
                                    if (!string.IsNullOrEmpty(chunk.Error))
                                    {
                                        await onError(chunk.Error);
                                        return;
                                    }

                                    if (chunk.ConversationId.HasValue)
                                    {
                                        await onConversationId(chunk.ConversationId.Value);
                                    }

                                    if (!string.IsNullOrEmpty(chunk.Content))
                                    {
                                        // FIX: Batch content updates
                                        contentBuffer.Append(chunk.Content);
                                        
                                        // Chỉ update UI theo interval hoặc khi có marker đặc biệt
                                        var now = DateTime.UtcNow;
                                        var shouldFlush = 
                                            (now - lastUpdateTime).TotalMilliseconds >= UPDATE_INTERVAL_MS ||
                                            chunk.Content.Contains("[TOOL_COMPLETE]") ||
                                            chunk.Content.Contains("⏳") ||
                                            chunk.Content.EndsWith("\n");

                                        if (shouldFlush && contentBuffer.Length > 0)
                                        {
                                            await onChunk(contentBuffer.ToString());
                                            contentBuffer.Clear();
                                            lastUpdateTime = now;
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"Failed to parse chunk: {ex.Message}, data: {data}");
                            }
                        }
                    }
                }

                // Flush any remaining content
                if (contentBuffer.Length > 0)
                {
                    await onChunk(contentBuffer.ToString());
                }
                
                await onComplete();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Stream cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stream error: {ex.Message}");
                await onError(ex.Message);
            }
        }

       
        public async Task<List<AiConversationSummaryDTO>> GetConversationsAsync()
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return new List<AiConversationSummaryDTO>();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/conversations");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<AiConversationSummaryDTO>();

                var result = await response.Content.ReadFromJsonAsync<List<AiConversationSummaryDTO>>();
                return result ?? new List<AiConversationSummaryDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversations: {ex.Message}");
                return new List<AiConversationSummaryDTO>();
            }
        }

                public async Task<AiConversationDTO?> GetConversationAsync(int id)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/conversations/{id}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<AiConversationDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversation {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteConversationAsync(int id)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return false;

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{API_BASE}/conversations/{id}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting conversation {id}: {ex.Message}");
                return false;
            }
        }
    }
}
