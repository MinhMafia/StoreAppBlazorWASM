 using StoreApp.Shared;
 
 namespace StoreApp.Services
 {
     /// <summary>
     /// Interface cho AI Service - dùng chung cho Admin và Customer
     /// </summary>
     public interface IAiService
     {
         /// <summary>
         /// Streaming chat với SSE
         /// </summary>
         IAsyncEnumerable<string> ChatStreamAsync(
             string userMessage,
             int userId,
             int? conversationId = null,
             List<ClientMessageDTO>? clientHistory = null,
             CancellationToken cancellationToken = default);
 
         /// <summary>
         /// Lấy danh sách conversations của user
         /// </summary>
         Task<List<AiConversationDTO>> GetConversationsAsync(int userId);
 
         /// <summary>
         /// Lấy chi tiết conversation
         /// </summary>
         Task<AiConversationDTO?> GetConversationAsync(int conversationId, int userId);
 
         /// <summary>
         /// Xóa conversation
         /// </summary>
         Task DeleteConversationAsync(int conversationId, int userId);
     }
 }
