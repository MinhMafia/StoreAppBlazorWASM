using Microsoft.AspNetCore.Mvc;
using StoreApp.Services.AI;
using StoreApp.Shared;
using System.Security.Claims;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/customer-ai")]
    public class CustomerAiController : ControllerBase
    {
        private readonly CustomerSemanticKernelService _aiService;
        private readonly ILogger<CustomerAiController> _logger;

        public CustomerAiController(CustomerSemanticKernelService aiService, ILogger<CustomerAiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        private int? GetAuthenticatedCustomerId()
        {
            var claim = User.FindFirst("customerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;

            return null;
        }

        private int GetSessionUserId(int? customerId)
        {
            return customerId ?? GetGuestUserId();
        }

        [HttpPost("stream")]
        public async Task StreamChat([FromBody] AiChatRequestDTO request)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("X-Accel-Buffering", "no");

            var authenticatedCustomerId = GetAuthenticatedCustomerId();
            var sessionUserId = GetSessionUserId(authenticatedCustomerId);

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

        private int GetGuestUserId()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var hash = ip.GetHashCode();
            return Math.Abs(hash % 1000000) + 1000000;
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
