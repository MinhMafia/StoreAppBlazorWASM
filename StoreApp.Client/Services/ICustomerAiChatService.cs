 using StoreApp.Shared;
 
 namespace StoreApp.Client.Services
 {
     /// <summary>
     /// Interface cho Customer AI Chat Service
     /// </summary>
     public interface ICustomerAiChatService
     {
         /// <summary>
         /// Stream chat message với SSE
         /// </summary>
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
 
         /// <summary>
         /// Lấy danh sách conversations
         /// </summary>
         Task<List<AiConversationSummaryDTO>> GetConversationsAsync();
 
         /// <summary>
         /// Lấy chi tiết conversation
         /// </summary>
         Task<AiConversationDTO?> GetConversationAsync(int id);
 
         /// <summary>
         /// Xóa conversation
         /// </summary>
         Task<bool> DeleteConversationAsync(int id);
     }
 }
