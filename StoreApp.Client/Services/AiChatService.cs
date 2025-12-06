using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using StoreApp.Shared;

namespace StoreApp.Client.Services
{
    /// <summary>
    /// Service xử lý AI Chat API - 100% C# không JS
    /// </summary>
    public class AiChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE = "api/ai";

        public AiChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gửi stream request qua HttpClient (C# approach)
        /// FIX: Xử lý SSE buffer đúng cách như React, batch UI updates
        /// </summary>
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

                // QUAN TRONG: Bat streaming cho Blazor WASM!
                // Mac dinh Blazor WASM buffer TOAN BO response truoc khi tra ve
                // Phai bat option nay de stream thuc su
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

                // FIX: Đọc stream với buffer như React - xử lý SSE format đúng cách
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                
                // Dùng buffer để handle SSE events tách bằng \n\n
                var buffer = new StringBuilder();
                var readBuffer = new byte[4096];
                var decoder = Encoding.UTF8;
                
                // Batch UI updates - accumulate chunks before updating
                var contentBuffer = new StringBuilder();
                var lastUpdateTime = DateTime.UtcNow;
                const int UPDATE_INTERVAL_MS = 50; // Update UI mỗi 50ms thay vì mỗi chunk

                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                    if (bytesRead == 0) break; // End of stream

                    // Decode và thêm vào buffer
                    buffer.Append(decoder.GetString(readBuffer, 0, bytesRead));

                    // Split bằng \n\n (SSE delimiter) - GIỐNG REACT
                    var bufferStr = buffer.ToString();
                    var events = bufferStr.Split(new[] { "\n\n" }, StringSplitOptions.None);
                    
                    // Giữ lại phần cuối (có thể chưa hoàn chỉnh)
                    buffer.Clear();
                    if (events.Length > 0)
                    {
                        buffer.Append(events[^1]); // Phần cuối chưa hoàn chỉnh
                    }

                    // Xử lý các events hoàn chỉnh
                    for (int i = 0; i < events.Length - 1; i++)
                    {
                        var eventData = events[i].Trim();
                        if (string.IsNullOrEmpty(eventData)) continue;

                        // Handle multi-line SSE (nếu có)
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

        /// <summary>
        /// Lấy danh sách conversations
        /// </summary>
        public async Task<List<AiConversationSummaryDTO>> GetConversationsAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<AiConversationSummaryDTO>>(
                    $"{API_BASE}/conversations"
                );
                return result ?? new List<AiConversationSummaryDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversations: {ex.Message}");
                return new List<AiConversationSummaryDTO>();
            }
        }

        /// <summary>
        /// Lấy chi tiết conversation
        /// </summary>
        public async Task<AiConversationDTO?> GetConversationAsync(int id)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<AiConversationDTO>(
                    $"{API_BASE}/conversations/{id}"
                );
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversation {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Xóa conversation
        /// </summary>
        public async Task<bool> DeleteConversationAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{API_BASE}/conversations/{id}");
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
