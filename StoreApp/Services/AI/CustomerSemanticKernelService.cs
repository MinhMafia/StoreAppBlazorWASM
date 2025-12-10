using StoreApp.Shared;
using StoreApp.Repository;
using StoreApp.Services.AI.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace StoreApp.Services.AI
{
    public class CustomerSemanticKernelService : IDisposable
    {
        private readonly AiRepository _aiRepository;
        private readonly ILogger<CustomerSemanticKernelService> _logger;
        private readonly TokenizerService _tokenizer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;

        private bool _disposed = false;

        public CustomerSemanticKernelService(
            AiRepository aiRepository,
            IConfiguration config,
            ILogger<CustomerSemanticKernelService> logger,
            IServiceProvider serviceProvider,
            TokenizerService tokenizer)
        {
            _aiRepository = aiRepository;
            _logger = logger;
            _tokenizer = tokenizer;
            _serviceProvider = serviceProvider;
            _config = config;

            _logger.LogInformation("CustomerSemanticKernelService initialized");
        }

        private (Kernel kernel, IChatCompletionService chatCompletion) CreateKernelWithPlugins(int? authenticatedCustomerId)
        {
            var apiKey = _config["Chutes:ApiKey"] 
                ?? throw new InvalidOperationException("Chutes API key not configured");
            var endpoint = new Uri(_config["Chutes:Endpoint"] ?? "https://llm.chutes.ai/v1");
            var model = _config["Chutes:Model"] ?? "deepseek-ai/DeepSeek-V3-0324";

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId: model, apiKey: apiKey, endpoint: endpoint);

            builder.Plugins.AddFromObject(new CustomerProductPlugin(_serviceProvider), "Product");
            builder.Plugins.AddFromObject(new CustomerCategoryPlugin(_serviceProvider), "Category");
            builder.Plugins.AddFromObject(new CustomerPromotionPlugin(_serviceProvider), "Promotion");
            builder.Plugins.AddFromObject(new CustomerOrderPlugin(_serviceProvider, authenticatedCustomerId), "Order");
            builder.Plugins.AddFromObject(new ProductSemanticSearchPlugin(_serviceProvider), "SemanticSearch");

            var kernel = builder.Build();
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

            return (kernel, chatCompletion);
        }

        #region Context Management

        private ChatHistory BuildManagedChatHistory(string systemPrompt, List<ClientMessageDTO>? clientHistory, string userMessage)
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);

            var systemTokens = _tokenizer.CountTokens(systemPrompt);
            var userTokens = _tokenizer.CountTokens(userMessage);
            var functionTokens = _tokenizer.EstimateFunctionTokens(4);

            var availableForHistory = AiConstants.ModelContextWindow 
                - AiConstants.MaxOutputTokens 
                - systemTokens 
                - userTokens 
                - functionTokens 
                - AiConstants.SafetyBuffer;

            if (clientHistory != null && clientHistory.Count > 0)
            {
                var selectedMessages = SelectHistoryWithTokenBudget(clientHistory, availableForHistory);
                foreach (var msg in selectedMessages)
                {
                    if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                        chatHistory.AddAssistantMessage(msg.Content);
                }
            }

            chatHistory.AddUserMessage(userMessage);
            return chatHistory;
        }

        private List<ClientMessageDTO> SelectHistoryWithTokenBudget(List<ClientMessageDTO> history, int tokenBudget)
        {
            var result = new List<ClientMessageDTO>();
            
            var windowed = history
                .Where(m => !string.IsNullOrEmpty(m.Content))
                .TakeLast(20)
                .ToList();

            var messagesWithTokens = windowed
                .Select(m => new
                {
                    Message = m,
                    Tokens = Math.Min(
                        _tokenizer.CountMessageTokens(m.Role, m.Content),
                        AiConstants.MaxSingleMessageTokens)
                })
                .ToList();

            var usedTokens = 0;
            var selectedIndices = new List<int>();

            for (int i = messagesWithTokens.Count - 1; i >= 0; i--)
            {
                var item = messagesWithTokens[i];
                if (usedTokens + item.Tokens <= tokenBudget)
                {
                    selectedIndices.Insert(0, i);
                    usedTokens += item.Tokens;
                }
                else break;
            }

            foreach (var idx in selectedIndices)
            {
                var msg = messagesWithTokens[idx].Message;
                var content = msg.Content;

                if (_tokenizer.CountTokens(content) > AiConstants.MaxSingleMessageTokens)
                    content = _tokenizer.TruncateToTokenLimit(content, AiConstants.MaxSingleMessageTokens);

                result.Add(new ClientMessageDTO { Role = msg.Role, Content = content });
            }

            return result;
        }

        private string TruncateToolResult(string result)
        {
            return _tokenizer.TruncateToTokenLimit(result, AiConstants.MaxToolResultTokens);
        }

        #endregion

        #region Chat Methods

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
            string? prepareError = null;

            try
            {
                if (!convId.HasValue)
                {
                    var title = GenerateConversationTitle(userMessage);
                    var conversation = await _aiRepository.CreateConversationAsync(userId, title);
                    convId = conversation.Id;
                }
                await _aiRepository.AddMessageAsync(convId.Value, "user", userMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing customer conversation");
                prepareError = GetUserFriendlyError(ex);
            }

            if (prepareError != null)
            {
                yield return $"❌ Lỗi: {prepareError}";
                yield break;
            }

            yield return $"convId:{convId}|";

            var (kernel, chatCompletion) = CreateKernelWithPlugins(authenticatedCustomerId);
            var fullResponse = new StringBuilder();
            var systemPrompt = GetCustomerSystemPrompt(authenticatedCustomerId.HasValue);
            var chatHistory = BuildManagedChatHistory(systemPrompt, clientHistory, userMessage);

            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false)
            };

            const int maxIterations = 10;
            int iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                AuthorRole? authorRole = null;
                var fccBuilder = new FunctionCallContentBuilder();

                await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(
                    chatHistory, settings, kernel, cancellationToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Content))
                    {
                        fullResponse.Append(chunk.Content);
                        yield return chunk.Content;
                    }
                    authorRole ??= chunk.Role;
                    fccBuilder.Append(chunk);
                }

                var functionCalls = fccBuilder.Build();
                if (!functionCalls.Any()) break;

                var funcNames = functionCalls.Select(f => f.FunctionName).Distinct().ToList();
                yield return $"\n\n⏳ *Đang tìm kiếm: {string.Join(", ", funcNames)}...*\n\n";

                var assistantMessage = new ChatMessageContent(role: authorRole ?? AuthorRole.Assistant, content: null);
                foreach (var fc in functionCalls)
                    assistantMessage.Items.Add(fc);
                chatHistory.Add(assistantMessage);

                var invokeTasks = functionCalls.Select(async fc =>
                {
                    try
                    {
                        var result = await fc.InvokeAsync(kernel, cancellationToken);
                        return (fc, result, (Exception?)null);
                    }
                    catch (Exception ex)
                    {
                        return (fc, (FunctionResultContent?)null, ex);
                    }
                });

                var results = await Task.WhenAll(invokeTasks);

                foreach (var (fc, result, error) in results)
                {
                    if (error != null)
                    {
                        chatHistory.Add(new FunctionResultContent(fc, $"Error: {error.Message}").ToChatMessage());
                    }
                    else if (result != null)
                    {
                        var resultStr = result.Result?.ToString() ?? "";
                        if (_tokenizer.CountTokens(resultStr) > AiConstants.MaxToolResultTokens)
                        {
                            var truncated = TruncateToolResult(resultStr);
                            chatHistory.Add(new FunctionResultContent(fc, truncated).ToChatMessage());
                        }
                        else
                        {
                            chatHistory.Add(result.ToChatMessage());
                        }
                    }
                }

                yield return "[TOOL_COMPLETE]";
            }

            if (fullResponse.Length > 0 && convId.HasValue)
            {
                try
                {
                    await _aiRepository.AddMessageAsync(convId.Value, "assistant", fullResponse.ToString());
                }
                catch { }
            }
        }

        public IAsyncEnumerable<string> ChatStreamAsync(
            string userMessage,
            int userId,
            int? conversationId = null,
            List<ClientMessageDTO>? clientHistory = null,
            CancellationToken cancellationToken = default)
        {
            return ChatStreamAsync(userMessage, userId, null, conversationId, clientHistory, cancellationToken);
        }

        #endregion

        #region Conversation Management

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

            if (userMessage.Length > 2000)
                return "Tin nhắn quá dài (tối đa 2000 ký tự)";

            return null;
        }

        private static string GenerateConversationTitle(string userMessage)
        {
            var title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage;
            return System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ").Trim();
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

        private static string GetCustomerSystemPrompt(bool isAuthenticated)
        {
            var today = DateTime.UtcNow.ToString("dd/MM/yyyy");
            var authStatus = isAuthenticated 
                ? "✅ Khách hàng ĐÃ ĐĂNG NHẬP - có thể xem đơn hàng của họ"
                : "❌ Khách hàng CHƯA ĐĂNG NHẬP - không thể xem đơn hàng";
            
            return $"""
                Bạn là trợ lý mua sắm AI của cửa hàng. Bạn giúp khách hàng tìm kiếm và mua sản phẩm.

                ## THÔNG TIN
                - Ngày hiện tại: {today}
                - Đơn vị tiền: VND (format: 1.500.000đ)
                - Ngôn ngữ: Tiếng Việt
                - Trạng thái đăng nhập: {authStatus}

                ## QUAN TRỌNG: KHI NÀO GỌI TOOL
                
                ❌ KHÔNG GỌI TOOL khi:
                - Chào hỏi: "xin chào", "hi", "hello"
                - Hỏi bạn là ai, bạn làm gì
                - Tâm sự, chat bình thường
                - Hỏi về thời tiết, tin tức, kiến thức chung
                - Cảm ơn, tạm biệt
                
                ✅ CHỈ GỌI TOOL khi khách HỎI CỤ THỂ về:
                - "Có sản phẩm gì?", "Tìm sản phẩm X", "Giá bao nhiêu?"
                - "Đơn hàng của tôi", "Kiểm tra đơn hàng số X"
                - "Có khuyến mãi gì?", "Mã giảm giá ABC"
                - "Danh mục sản phẩm"

                ## NHIỆM VỤ (chỉ dùng khi được hỏi)
                1. Tìm kiếm và tư vấn sản phẩm → SearchProducts, GetProductDetail
                2. Tra cứu đơn hàng của khách → GetMyOrders, GetOrderDetail (CHỈ khi đã đăng nhập)
                3. Kiểm tra mã khuyến mãi → CheckPromotion
                4. Xem danh mục → GetCategories

                ## QUY TẮC XỬ LÝ ĐƠN HÀNG
                - Nếu khách ĐÃ ĐĂNG NHẬP và hỏi "tra cứu đơn hàng", "đơn hàng của tôi" → GỌI TOOL GetMyOrders NGAY
                - Nếu khách CHƯA ĐĂNG NHẬP mà hỏi về đơn hàng → Trả lời: "Để xem đơn hàng, bạn vui lòng đăng nhập vào tài khoản."

                ## QUY TẮC CHUNG
                - Câu hỏi chung: Trả lời trực tiếp, KHÔNG gọi tool
                - Cần dữ liệu sản phẩm/đơn hàng: Gọi tool phù hợp
                - Không tìm thấy: Nói thật "Không tìm thấy"
                - KHÔNG bịa tên sản phẩm, giá tiền
                - Luôn thân thiện, lịch sự
                - Ưu tiên sản phẩm còn hàng

                ## ĐỊNH DẠNG TRẢ LỜI
                - KHÔNG dùng bảng markdown (table) vì khung chat nhỏ
                - Dùng danh sách với emoji: 🛒 **Tên SP** - Giá (còn X)
                - Giữ câu trả lời ngắn gọn, dễ đọc trên mobile

                ## GIỚI HẠN
                - KHÔNG tiết lộ thông tin nội bộ cửa hàng
                - KHÔNG xem đơn hàng của người khác
                - KHÔNG thực hiện thanh toán hay tạo đơn hàng
                - Câu hỏi ngoài phạm vi: lịch sự từ chối
                """;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
