using System.Text.Json.Serialization;

namespace StoreApp.Client.Models.AI
{
    /// <summary>
    /// Request model cho chat API
    /// </summary>
    public class AiChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("conversationId")]
        public int? ConversationId { get; set; }

        [JsonPropertyName("history")]
        public List<ClientMessage>? History { get; set; }
    }

    /// <summary>
    /// Message trong history gửi lên server
    /// </summary>
    public class ClientMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response chunk từ SSE stream
    /// </summary>
    public class StreamChunk
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("conversationId")]
        public int? ConversationId { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
