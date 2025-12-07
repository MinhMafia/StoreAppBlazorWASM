 using StoreApp.Shared;
 using StoreApp.Repository;
 using StoreApp.Services.AI;
 using OpenAI;
 using OpenAI.Chat;
 using System.ClientModel;
 using System.Runtime.CompilerServices;
 using System.Text;
 using System.Text.Json;
 
 namespace StoreApp.Services
 {
     /// <summary>
     /// AI Chat Service cho Customer - giới hạn tools và data access
     /// Kế thừa logic từ AiService, chỉ thay đổi system prompt và tools
     /// </summary>
     public class CustomerAiService : IAiService, IDisposable
     {
         private readonly AiRepository _aiRepository;
         private readonly CustomerAiToolExecutor _toolExecutor;
         private readonly ILogger<CustomerAiService> _logger;
         private readonly ChatClient _chatClient;
         private readonly TokenizerService _tokenizer;
         private readonly ChatContextManager _contextManager;
 
         private bool _disposed = false;
 
         public CustomerAiService(
             AiRepository aiRepository,
             CustomerAiToolExecutor toolExecutor,
             IConfiguration config,
             ILogger<CustomerAiService> logger,
             TokenizerService tokenizer,
             ChatContextManager contextManager)
         {
             _aiRepository = aiRepository;
             _toolExecutor = toolExecutor;
             _logger = logger;
             _tokenizer = tokenizer;
             _contextManager = contextManager;
 
             var apiKey = config["Chutes:ApiKey"] ?? throw new InvalidOperationException("Chutes API key not configured");
             var endpoint = new Uri("https://llm.chutes.ai/v1");
             var model = "moonshotai/Kimi-K2-Instruct-0905";
 
             _chatClient = new ChatClient(
                 model: model,
                 credential: new ApiKeyCredential(apiKey),
                 options: new OpenAIClientOptions { Endpoint = endpoint }
             );
 
             _logger.LogInformation("Customer AI Service initialized");
         }
 
         #region IAiService Implementation
 
        /// <summary>
        /// Streaming chat với authenticatedCustomerId để phân biệt user thật vs guest
        /// </summary>
         public async IAsyncEnumerable<string> ChatStreamAsync(
             string userMessage,
             int userId,
            int? authenticatedCustomerId,
             int? conversationId = null,
             List<ClientMessageDTO>? clientHistory = null,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
         {
             var validationError = ValidateUserMessage(userMessage);
             if (validationError != null)
             {
                 yield return $"⚠️ {validationError}";
                 yield break;
             }
 
             int? convId = conversationId;
             var fullResponse = new StringBuilder();
             string? errorMessage = null;
 
             List<ChatMessage>? messages = null;
             ChatCompletionOptions? options = null;
             string? prepareError = null;
 
             try
             {
                 messages = _contextManager.BuildMessages(GetCustomerSystemPrompt(), clientHistory, userMessage);
 
                 if (!convId.HasValue)
                 {
                     var title = GenerateConversationTitle(userMessage);
                     var conversation = await _aiRepository.CreateConversationAsync(userId, title);
                     convId = conversation.Id;
                 }
 
                 await _aiRepository.AddMessageAsync(convId.Value, "user", userMessage);
 
                 var tools = CustomerAiToolDefinitions.GetAll();
                 options = new ChatCompletionOptions
                 {
                     MaxOutputTokenCount = ChatContextManager.MAX_OUTPUT_TOKENS
                 };
                 foreach (var tool in tools)
                 {
                     options.Tools.Add(tool);
                 }
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error preparing customer conversation for user {UserId}", userId);
                 prepareError = GetUserFriendlyError(ex);
             }
 
             if (prepareError != null)
             {
                 yield return $"❌ Lỗi: {prepareError}";
                 yield break;
             }
 
             if (messages == null || options == null)
             {
                 yield return "❌ Lỗi khởi tạo";
                 yield break;
             }
 
             yield return $"convId:{convId}|";
 
             _logger.LogInformation("Starting Customer AI stream for user {UserId}, convId: {ConvId}", userId, convId);
            
            // Dùng authenticatedCustomerId (null nếu guest) để check quyền xem đơn hàng
            var customerIdForTools = authenticatedCustomerId;
 
             bool requiresAction = true;
 
             while (requiresAction && errorMessage == null)
             {
                 requiresAction = false;
 
                 var contentBuilder = new StringBuilder();
                 var toolCallsBuilder = new StreamingChatToolCallsBuilder();
                 bool toolCallStarted = false;
 
                 await foreach (var streamResult in SafeStreamAsync(messages, options, cancellationToken))
                 {
                     if (streamResult.Error != null)
                     {
                         errorMessage = streamResult.Error;
                         break;
                     }
 
                     if (streamResult.Chunk != null)
                     {
                         foreach (var contentPart in streamResult.Chunk.ContentUpdate)
                         {
                             if (!string.IsNullOrEmpty(contentPart.Text))
                             {
                                 contentBuilder.Append(contentPart.Text);
                                 yield return contentPart.Text;
                             }
                         }
 
                         foreach (var toolCallUpdate in streamResult.Chunk.ToolCallUpdates)
                         {
                             if (!toolCallStarted)
                             {
                                 toolCallStarted = true;
                             }
                             toolCallsBuilder.Append(toolCallUpdate);
                         }
                     }
                 }
 
                 if (errorMessage != null) break;
 
                 var toolCalls = toolCallsBuilder.Build();
                 if (toolCalls.Count > 0)
                 {
                     requiresAction = true;
 
                     var toolNames = toolCalls.Select(t => CustomerAiToolNames.GetDisplayName(t.FunctionName)).Distinct();
                     yield return $"\n\n⏳ *Đang tìm kiếm: {string.Join(", ", toolNames)}...*\n\n";
 
                     var assistantMessage = new AssistantChatMessage(toolCalls);
                     if (contentBuilder.Length > 0)
                     {
                         assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                     }
                     messages.Add(assistantMessage);
 
                    // Execute tools với authenticatedCustomerId (null nếu guest)
                     var toolCallData = toolCalls.Select(tc => (tc.Id, tc.FunctionName, tc.FunctionArguments.ToString()));
                    var toolResults = await _toolExecutor.ExecuteParallelAsync(toolCallData, customerIdForTools);
 
                     foreach (var (toolCallId, result) in toolResults)
                     {
                         var truncatedResult = _contextManager.TruncateToolResult(result, AiConstants.MaxToolResultTokens);
                         messages.Add(new ToolChatMessage(toolCallId, truncatedResult));
                     }
 
                     yield return "[TOOL_COMPLETE]";
                 }
                 else
                 {
                     fullResponse.Append(contentBuilder);
                 }
             }
 
             if (errorMessage != null)
             {
                 yield return $"\n\n*{errorMessage}*";
             }
 
             if (fullResponse.Length > 0 && convId.HasValue)
             {
                 try
                 {
                     await _aiRepository.AddMessageAsync(convId.Value, "assistant", fullResponse.ToString());
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Error saving customer AI response");
                 }
             }
         }
 
        /// <summary>
        /// IAiService implementation - gọi overload với authenticatedCustomerId = null (guest)
        /// </summary>
        public IAsyncEnumerable<string> ChatStreamAsync(
            string userMessage,
            int userId,
            int? conversationId = null,
            List<ClientMessageDTO>? clientHistory = null,
            CancellationToken cancellationToken = default)
        {
            // Fallback: không có authenticated customer (guest mode)
            return ChatStreamAsync(userMessage, userId, null, conversationId, clientHistory, cancellationToken);
        }

         public async Task<List<AiConversationDTO>> GetConversationsAsync(int userId)
         {
             var conversations = await _aiRepository.GetConversationsByUserIdAsync(userId);
             return conversations.Select(c => new AiConversationDTO
             {
                 Id = c.Id,
                 Title = c.Title,
                 CreatedAt = c.CreatedAt,
                 UpdatedAt = c.UpdatedAt
             }).ToList();
         }
 
         public async Task<AiConversationDTO?> GetConversationAsync(int conversationId, int userId)
         {
             var conversation = await _aiRepository.GetConversationByIdAsync(conversationId, userId);
             if (conversation == null) return null;
 
             return new AiConversationDTO
             {
                 Id = conversation.Id,
                 Title = conversation.Title,
                 CreatedAt = conversation.CreatedAt,
                 UpdatedAt = conversation.UpdatedAt,
                 Messages = conversation.Messages.Select(m => new AiMessageDTO
                 {
                     Id = m.Id,
                     Role = m.Role,
                     Content = m.Content,
                     CreatedAt = m.CreatedAt
                 }).ToList()
             };
         }
 
         public async Task DeleteConversationAsync(int conversationId, int userId)
         {
             await _aiRepository.DeleteConversationAsync(conversationId, userId);
         }
 
         #endregion
 
         #region Private Methods
 
         private static string? ValidateUserMessage(string userMessage)
         {
             if (string.IsNullOrWhiteSpace(userMessage))
                 return "Vui lòng nhập câu hỏi";
 
             if (userMessage.Length > AiConstants.MaxMessageLength)
                 return $"Tin nhắn quá dài (tối đa {AiConstants.MaxMessageLength:N0} ký tự)";
 
             return null;
         }
 
         private async IAsyncEnumerable<StreamResult> SafeStreamAsync(
             List<ChatMessage> messages,
             ChatCompletionOptions options,
             [EnumeratorCancellation] CancellationToken cancellationToken)
         {
             IAsyncEnumerator<StreamingChatCompletionUpdate>? enumerator = null;
             StreamResult? errorResult = null;
 
             try
             {
                 var stream = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);
                 enumerator = stream.GetAsyncEnumerator(cancellationToken);
 
                 while (errorResult == null)
                 {
                     bool hasNext;
                     StreamResult? moveNextError = null;
 
                     try
                     {
                         hasNext = await enumerator.MoveNextAsync();
                     }
                     catch (OperationCanceledException)
                     {
                         moveNextError = new StreamResult { Error = "Đã hủy" };
                         hasNext = false;
                     }
                     catch (Exception ex)
                     {
                         _logger.LogError(ex, "Customer stream error");
                         moveNextError = new StreamResult { Error = GetUserFriendlyError(ex) };
                         hasNext = false;
                     }
 
                     if (moveNextError != null)
                     {
                         errorResult = moveNextError;
                         break;
                     }
 
                     if (!hasNext) break;
 
                     yield return new StreamResult { Chunk = enumerator.Current };
                 }
             }
             finally
             {
                 if (enumerator != null)
                 {
                     await enumerator.DisposeAsync();
                 }
             }
 
             if (errorResult != null)
             {
                 yield return errorResult;
             }
         }
 
         private static string GenerateConversationTitle(string userMessage)
         {
             var title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage;
             title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ").Trim();
             return title;
         }
 
         private static string GetUserFriendlyError(Exception ex)
         {
             if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                 return "Hệ thống đang bận, vui lòng thử lại sau";
             if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                 return "Kết nối quá chậm, vui lòng thử lại";
             if (ex is HttpRequestException)
                 return "Không thể kết nối đến AI service";
 
             return "Đã xảy ra lỗi, vui lòng thử lại";
         }
 
        private static string GetCustomerSystemPrompt()
        {
            var today = DateTime.UtcNow.ToString("dd/MM/yyyy");
            return $"""
                Bạn là trợ lý mua sắm AI của cửa hàng. Bạn giúp khách hàng:

                ## NHIỆM VỤ
                1. Tìm kiếm và tư vấn sản phẩm phù hợp
                2. Tra cứu thông tin đơn hàng của khách (CHỈ KHI ĐÃ ĐĂNG NHẬP)
                3. Kiểm tra mã khuyến mãi
                4. Trả lời câu hỏi về sản phẩm, giá cả

                ## THÔNG TIN
                - Ngày hiện tại: {today}
                - Đơn vị tiền: VND (format: 1.500.000đ)
                - Ngôn ngữ: Tiếng Việt

                ## QUY TẮC BẮT BUỘC VỀ TOOLS (RẤT QUAN TRỌNG)
                ⚠️ BẠN KHÔNG BIẾT BẤT KỲ SẢN PHẨM NÀO TRONG CỬA HÀNG!
                ⚠️ BẠN PHẢI GỌI TOOL ĐỂ LẤY DỮ LIỆU THỰC TỪ DATABASE!
                
                LUÔN LUÔN gọi tool trong các trường hợp sau:
                - Khách hỏi "có sản phẩm gì?", "bán gì?", "có hàng gì?" → GỌI search_products
                - Khách hỏi về một sản phẩm cụ thể → GỌI get_product_detail hoặc search_products
                - Khách hỏi về giá → GỌI search_products hoặc get_product_detail
                - Khách hỏi về danh mục → GỌI get_categories
                - Khách hỏi về khuyến mãi → GỌI check_promotion
                - Khách hỏi về đơn hàng → GỌI get_my_orders hoặc get_order_detail

                TUYỆT ĐỐI KHÔNG ĐƯỢC:
                - Bịa ra tên sản phẩm
                - Bịa ra giá tiền
                - Bịa ra thông tin không có trong kết quả tool
                - Trả lời về sản phẩm mà KHÔNG gọi tool trước

                ## QUY TẮC KHÁC
                1. Luôn thân thiện, lịch sự với khách hàng
                2. Khi gợi ý sản phẩm, ưu tiên sản phẩm còn hàng (InStock = true)
                3. Định dạng tiền tệ đẹp (1.500.000đ)
                4. Trả lời ngắn gọn, dễ hiểu
                5. Nếu khách hỏi về đơn hàng, yêu cầu họ cung cấp mã đơn

                ## GIỚI HẠN
                - KHÔNG tiết lộ thông tin nội bộ cửa hàng
                - KHÔNG xem đơn hàng của người khác
                - KHÔNG thực hiện thanh toán hay tạo đơn hàng
                - Nếu khách hỏi ngoài phạm vi, lịch sự từ chối

                ## BẢO MẬT THÔNG TIN CÁ NHÂN
                ⚠️ TUYỆT ĐỐI KHÔNG được trả lời các câu hỏi liên quan đến THÔNG TIN CÁ NHÂN nếu khách hàng CHƯA ĐĂNG NHẬP:
                - Thông tin đơn hàng (lịch sử đơn hàng, trạng thái đơn, chi tiết đơn hàng)
                - Thông tin tài khoản (tên, email, số điện thoại, địa chỉ)
                - Lịch sử mua hàng
                - Điểm tích lũy, ưu đãi cá nhân

                Khi khách hàng CHƯA ĐĂNG NHẬP mà hỏi về thông tin cá nhân:
                1. Lịch sự thông báo: "Để xem thông tin này, bạn vui lòng đăng nhập vào tài khoản."
                2. Hướng dẫn họ đăng nhập hoặc đăng ký tài khoản

                Lưu ý: Nếu tool trả về lỗi "chưa đăng nhập" hoặc "không có quyền", hãy hướng dẫn khách đăng nhập.
                """;
        }
 
         #endregion
 
         #region IDisposable
 
         public void Dispose()
         {
             Dispose(true);
             GC.SuppressFinalize(this);
         }
 
         protected virtual void Dispose(bool disposing)
         {
             if (!_disposed)
             {
                 _disposed = true;
             }
         }
 
         #endregion
 
         #region Helper Classes
 
         private class StreamResult
         {
             public StreamingChatCompletionUpdate? Chunk { get; set; }
             public string? Error { get; set; }
         }
 
         #endregion
     }
 }
