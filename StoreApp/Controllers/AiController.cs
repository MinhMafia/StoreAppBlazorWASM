using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StoreApp.Services.AI;
using StoreApp.Shared;
using System.Security.Claims;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,staff")]
    public class AiController : ControllerBase
    {
        private readonly SemanticKernelService _aiService;
        private readonly ILogger<AiController> _logger;

        public AiController(SemanticKernelService aiService, ILogger<AiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
            return 0;
        }

        [HttpPost("stream")]
        public async Task StreamChat([FromBody] AiChatRequestDTO request)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("X-Accel-Buffering", "no");

            var userId = GetCurrentUserId();

            if (userId == 0)
            {
                await SendStreamError("Vui lòng đăng nhập");
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                await SendStreamError("Vui lòng nhập câu hỏi");
                return;
            }

            if (request.Message.Length > 4000)
            {
                await SendStreamError("Tin nhắn quá dài (tối đa 4000 ký tự)");
                return;
            }

            if (request.History != null && request.History.Count > 50)
            {
                await SendStreamError("Lịch sử chat quá dài");
                return;
            }

            try
            {
                _logger.LogInformation("User {UserId} starting stream chat", userId);

                var cancellationToken = HttpContext.RequestAborted;

                await foreach (var streamChunk in _aiService.ChatStreamAsync(
                    request.Message,
                    userId,
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
                _logger.LogInformation("Stream cancelled for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StreamChat for user {UserId}: {Message}", userId, ex.Message);
                await SendStreamError($"Lỗi: {ex.Message}");
            }

            await SendStreamDone();
        }

        [HttpGet("conversations")]
        [ProducesResponseType(typeof(List<AiConversationDTO>), 200)]
        public async Task<ActionResult<List<AiConversationDTO>>> GetConversations()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var conversations = await _aiService.GetConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                return StatusCode(500, new { error = "Không thể tải danh sách hội thoại" });
            }
        }

        [HttpGet("conversations/{id:int}")]
        [ProducesResponseType(typeof(AiConversationDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AiConversationDTO>> GetConversation(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "ID không hợp lệ" });
            }

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var conversation = await _aiService.GetConversationAsync(id, userId);
                if (conversation == null)
                    return NotFound(new { error = "Không tìm thấy cuộc hội thoại" });

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId} for user {UserId}", id, userId);
                return StatusCode(500, new { error = "Không thể tải cuộc hội thoại" });
            }
        }

        [HttpDelete("conversations/{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteConversation(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "ID không hợp lệ" });
            }

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                await _aiService.DeleteConversationAsync(id, userId);
                return Ok(new { success = true, message = "Đã xóa cuộc hội thoại" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
                return StatusCode(500, new { error = "Không thể xóa cuộc hội thoại" });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            var stats = _aiService.GetPluginStats();
            return Ok(new
            {
                status = "ok",
                message = "AI Service (Semantic Kernel) is running",
                plugins = stats.pluginCount,
                functions = stats.functionCount,
                timestamp = DateTime.UtcNow
            });
        }

        #region Stream Helpers

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
