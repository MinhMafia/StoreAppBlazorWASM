 using Microsoft.AspNetCore.Mvc;
 using StoreApp.Services;
 using StoreApp.Shared;
 using System.Security.Claims;
 
 namespace StoreApp.Controllers
 {
     /// <summary>
     /// API Controller cho Customer AI Chat
     /// Endpoint riêng biệt với Admin để đảm bảo phân quyền
     /// </summary>
     [ApiController]
     [Route("api/customer-ai")]
     public class CustomerAiController : ControllerBase
     {
         private readonly CustomerAiService _aiService;
         private readonly ILogger<CustomerAiController> _logger;
 
         public CustomerAiController(CustomerAiService aiService, ILogger<CustomerAiController> logger)
         {
             _aiService = aiService;
             _logger = logger;
         }
 
         /// <summary>
        /// Lấy customer ID thực từ JWT token (null nếu chưa đăng nhập)
         /// </summary>
        private int? GetAuthenticatedCustomerId()
         {
             var claim = User.FindFirst("customerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
             if (claim != null && int.TryParse(claim.Value, out int id))
                 return id;
 
            return null;
        }

        /// <summary>
        /// Lấy user ID cho lưu conversation (customerId nếu đã login, hoặc hash IP nếu guest)
        /// </summary>
        private int GetSessionUserId(int? customerId)
        {
            return customerId ?? GetGuestUserId();
         }
 
         /// <summary>
         /// Streaming chat endpoint cho Customer
         /// </summary>
         [HttpPost("stream")]
         public async Task StreamChat([FromBody] AiChatRequestDTO request)
         {
             Response.Headers.Append("Content-Type", "text/event-stream");
             Response.Headers.Append("Cache-Control", "no-cache");
             Response.Headers.Append("Connection", "keep-alive");
             Response.Headers.Append("X-Accel-Buffering", "no");
 
            // Lấy customerId thực từ JWT (null nếu guest)
            var authenticatedCustomerId = GetAuthenticatedCustomerId();
            // Dùng cho lưu conversation
            var sessionUserId = GetSessionUserId(authenticatedCustomerId);
 
             // Validation
             if (request == null || string.IsNullOrWhiteSpace(request.Message))
             {
                 await SendStreamError("Vui lòng nhập câu hỏi");
                 return;
             }
 
             if (request.Message.Length > 2000)
             {
                 await SendStreamError("Tin nhắn quá dài (tối đa 2000 ký tự)");
                 return;
             }
 
             if (request.History != null && request.History.Count > 20)
             {
                 await SendStreamError("Lịch sử chat quá dài");
                 return;
             }
 
             try
             {
                _logger.LogInformation("Customer {CustomerId} (authenticated: {IsAuth}) starting chat", 
                    sessionUserId, authenticatedCustomerId.HasValue);
 
                 var cancellationToken = HttpContext.RequestAborted;
 
                 await foreach (var streamChunk in _aiService.ChatStreamAsync(
                     request.Message,
                    sessionUserId,
                    authenticatedCustomerId,
                     request.ConversationId,
                     request.History).WithCancellation(cancellationToken))
                 {
                     if (streamChunk.StartsWith("convId:"))
                     {
                         var pipeIndex = streamChunk.IndexOf('|');
                         if (pipeIndex > 0)
                         {
                             var convIdStr = streamChunk.Substring(7, pipeIndex - 7);
                             var content = streamChunk.Substring(pipeIndex + 1);
 
                             await SendStreamData(new { conversationId = int.Parse(convIdStr) });
 
                             if (!string.IsNullOrEmpty(content))
                             {
                                 await SendStreamData(new { content });
                             }
                             continue;
                         }
                     }
 
                     await SendStreamData(new { content = streamChunk });
                 }
             }
             catch (OperationCanceledException)
             {
                 _logger.LogInformation("Customer stream cancelled");
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error in Customer StreamChat");
                 await SendStreamError("Đã xảy ra lỗi, vui lòng thử lại");
             }
 
             await SendStreamDone();
         }
 
         /// <summary>
         /// Lấy danh sách conversations của customer
         /// </summary>
         [HttpGet("conversations")]
         public async Task<ActionResult<List<AiConversationDTO>>> GetConversations()
         {
            var authenticatedCustomerId = GetAuthenticatedCustomerId();
            var sessionUserId = GetSessionUserId(authenticatedCustomerId);
 
             try
             {
                var conversations = await _aiService.GetConversationsAsync(sessionUserId);
                 return Ok(conversations);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error getting customer conversations");
                 return StatusCode(500, new { error = "Không thể tải lịch sử" });
             }
         }
 
         /// <summary>
         /// Lấy chi tiết conversation
         /// </summary>
         [HttpGet("conversations/{id:int}")]
         public async Task<ActionResult<AiConversationDTO>> GetConversation(int id)
         {
             if (id <= 0)
                 return BadRequest(new { error = "ID không hợp lệ" });
 
            var authenticatedCustomerId = GetAuthenticatedCustomerId();
            var sessionUserId = GetSessionUserId(authenticatedCustomerId);
 
             try
             {
                var conversation = await _aiService.GetConversationAsync(id, sessionUserId);
                 if (conversation == null)
                     return NotFound(new { error = "Không tìm thấy" });
 
                 return Ok(conversation);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error getting customer conversation {Id}", id);
                 return StatusCode(500, new { error = "Không thể tải hội thoại" });
             }
         }
 
         /// <summary>
         /// Xóa conversation
         /// </summary>
         [HttpDelete("conversations/{id:int}")]
         public async Task<ActionResult> DeleteConversation(int id)
         {
             if (id <= 0)
                 return BadRequest(new { error = "ID không hợp lệ" });
 
            var authenticatedCustomerId = GetAuthenticatedCustomerId();
            var sessionUserId = GetSessionUserId(authenticatedCustomerId);
 
             try
             {
                await _aiService.DeleteConversationAsync(id, sessionUserId);
                 return Ok(new { success = true });
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error deleting customer conversation {Id}", id);
                 return StatusCode(500, new { error = "Không thể xóa" });
             }
         }
 
         #region Private Helpers
 
         /// <summary>
         /// Tạo userId cho guest dựa trên IP hoặc session
         /// </summary>
         private int GetGuestUserId()
         {
             var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
             var hash = ip.GetHashCode();
             // Đảm bảo số dương và trong range hợp lý
             return Math.Abs(hash % 1000000) + 1000000; // 1000000 - 1999999
         }
 
         private async Task SendStreamData(object data)
         {
             var json = System.Text.Json.JsonSerializer.Serialize(data);
             await Response.WriteAsync($"data: {json}\n\n");
             await Response.Body.FlushAsync();
         }
 
         private async Task SendStreamError(string error)
         {
             await SendStreamData(new { error });
             await SendStreamDone();
         }
 
         private async Task SendStreamDone()
         {
             await Response.WriteAsync("data: [DONE]\n\n");
             await Response.Body.FlushAsync();
         }
 
         #endregion
     }
 }
