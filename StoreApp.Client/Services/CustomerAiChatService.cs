 using System.Net.Http.Json;
 using System.Text;
 using System.Text.Json;
 using Microsoft.AspNetCore.Components.WebAssembly.Http;
 using Blazored.LocalStorage;
 using StoreApp.Shared;
 
 namespace StoreApp.Client.Services
 {
     /// <summary>
     /// Service xử lý Customer AI Chat API
     /// Kết nối đến endpoint /api/customer-ai
     /// </summary>
     public class CustomerAiChatService : ICustomerAiChatService
     {
         private readonly HttpClient _httpClient;
         private readonly ILocalStorageService _localStorage;
         private const string API_BASE = "api/customer-ai";
 
         public CustomerAiChatService(HttpClient httpClient, ILocalStorageService localStorage)
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

                 var token = await GetAuthTokenAsync();
                 if (!string.IsNullOrEmpty(token))
                 {
                     httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                 }
 
                 httpRequest.SetBrowserResponseStreamingEnabled(true);
 
                 using var response = await _httpClient.SendAsync(
                     httpRequest,
                     HttpCompletionOption.ResponseHeadersRead,
                     cancellationToken
                 );
 
                 if (!response.IsSuccessStatusCode)
                 {
                     var error = await response.Content.ReadAsStringAsync();
                     await onError($"Lỗi: {response.StatusCode}");
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
                     if (bytesRead == 0) break;
 
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
                                         contentBuffer.Append(chunk.Content);
 
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
                                 Console.WriteLine($"Failed to parse chunk: {ex.Message}");
                             }
                         }
                     }
                 }
 
                 if (contentBuffer.Length > 0)
                 {
                     await onChunk(contentBuffer.ToString());
                 }
 
                 await onComplete();
             }
             catch (OperationCanceledException)
             {
                 Console.WriteLine("Customer stream cancelled");
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Customer stream error: {ex.Message}");
                 await onError("Đã xảy ra lỗi, vui lòng thử lại");
             }
         }
 
         public async Task<List<AiConversationSummaryDTO>> GetConversationsAsync()
         {
             try
             {
                 var token = await GetAuthTokenAsync();
                 var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/conversations");
                 if (!string.IsNullOrEmpty(token))
                 {
                     request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                 }

                 var response = await _httpClient.SendAsync(request);
                 if (response.IsSuccessStatusCode)
                 {
                     return await response.Content.ReadFromJsonAsync<List<AiConversationSummaryDTO>>() ?? new List<AiConversationSummaryDTO>();
                 }
                 return new List<AiConversationSummaryDTO>();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Error getting customer conversations: {ex.Message}");
                 return new List<AiConversationSummaryDTO>();
             }
         }
 
         public async Task<AiConversationDTO?> GetConversationAsync(int id)
         {
             try
             {
                 var token = await GetAuthTokenAsync();
                 var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/conversations/{id}");
                 if (!string.IsNullOrEmpty(token))
                 {
                     request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                 }

                 var response = await _httpClient.SendAsync(request);
                 if (response.IsSuccessStatusCode)
                 {
                     return await response.Content.ReadFromJsonAsync<AiConversationDTO>();
                 }
                 return null;
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Error getting customer conversation {id}: {ex.Message}");
                 return null;
             }
         }
 
         public async Task<bool> DeleteConversationAsync(int id)
         {
             try
             {
                 var token = await GetAuthTokenAsync();
                 var request = new HttpRequestMessage(HttpMethod.Delete, $"{API_BASE}/conversations/{id}");
                 if (!string.IsNullOrEmpty(token))
                 {
                     request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                 }

                 var response = await _httpClient.SendAsync(request);
                 return response.IsSuccessStatusCode;
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Error deleting customer conversation {id}: {ex.Message}");
                 return false;
             }
         }
     }
 }
