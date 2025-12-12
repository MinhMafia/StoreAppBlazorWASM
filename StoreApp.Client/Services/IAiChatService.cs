using StoreApp.Shared;

namespace StoreApp.Client.Services
{
  
    public interface IAiChatService
    {
               Task StreamMessageAsync(
            string message,
            int? conversationId,
            List<ClientMessageDTO>? history,
            Func<string, Task> onChunk,
            Func<int, Task> onConversationId,
            Func<string, Task> onError,
            Func<Task> onComplete,
            CancellationToken cancellationToken = default
        );

      
        Task<List<AiConversationSummaryDTO>> GetConversationsAsync();

      
        Task<AiConversationDTO?> GetConversationAsync(int id);

        Task<bool> DeleteConversationAsync(int id);
    }
}
