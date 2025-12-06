using StoreApp.Client.Models.AI;

namespace StoreApp.Client.Services
{
    /// <summary>
    /// Interface cho AI Chat Service
    /// </summary>
    public interface IAiChatService
    {
        /// <summary>
        /// Stream chat message với SSE (C# approach)
        /// </summary>
        Task StreamMessageAsync(
            string message,
            int? conversationId,
            List<ClientMessage>? history,
            Func<string, Task> onChunk,
            Func<int, Task> onConversationId,
            Func<string, Task> onError,
            Func<Task> onComplete,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lấy danh sách conversations
        /// </summary>
        Task<List<ConversationSummary>> GetConversationsAsync();

        /// <summary>
        /// Lấy chi tiết conversation
        /// </summary>
        Task<ConversationDetail?> GetConversationAsync(int id);

        /// <summary>
        /// Xóa conversation
        /// </summary>
        Task<bool> DeleteConversationAsync(int id);
    }
}
