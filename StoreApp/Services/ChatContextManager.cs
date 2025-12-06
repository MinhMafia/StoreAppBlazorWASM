using StoreApp.Shared;
using StoreApp.Services.AI;
using OpenAI.Chat;

namespace StoreApp.Services
{
    /// <summary>
    /// Quản lý context cho AI chat theo chuẩn industry best practices
    ///
    /// Strategies được áp dụng:
    /// 1. Sliding Window: Giữ N tin nhắn gần nhất
    /// 2. Token Budget: Phân bổ token cho system, history, user, output
    /// 3. Smart Truncation: Cắt tin nhắn cũ khi vượt budget
    /// 4. Summarization Ready: Cấu trúc hỗ trợ tóm tắt (future feature)
    /// </summary>
    public class ChatContextManager
    {
        private readonly TokenizerService _tokenizer;
        private readonly ILogger<ChatContextManager> _logger;

        #region Configuration - Using centralized AiConstants

        // Expose constants for backward compatibility
        public static int MODEL_CONTEXT_WINDOW => AiConstants.ModelContextWindow;
        public static int MAX_OUTPUT_TOKENS => AiConstants.MaxOutputTokens;
        public static int SAFETY_BUFFER => AiConstants.SafetyBuffer;
        public static int MAX_HISTORY_MESSAGES => AiConstants.MaxHistoryMessages;
        public static int MAX_SINGLE_MESSAGE_TOKENS => AiConstants.MaxSingleMessageTokens;
        public static int ESTIMATED_FUNCTION_COUNT => AiConstants.EstimatedFunctionCount;

        #endregion

        #region Calculated Budgets

        /// <summary>
        /// Token budget cho history messages
        /// = Context - Output - Functions - Buffer
        /// </summary>
        public int HistoryTokenBudget => MODEL_CONTEXT_WINDOW
            - MAX_OUTPUT_TOKENS
            - _tokenizer.EstimateFunctionTokens(ESTIMATED_FUNCTION_COUNT)
            - SAFETY_BUFFER;

        #endregion

        public ChatContextManager(TokenizerService tokenizer, ILogger<ChatContextManager> logger)
        {
            _tokenizer = tokenizer;
            _logger = logger;
        }

        /// <summary>
        /// Build messages list với context management
        ///
        /// Flow:
        /// 1. Add system prompt
        /// 2. Calculate available tokens for history
        /// 3. Apply sliding window (max N messages)
        /// 4. Apply token budget (fit within limit)
        /// 5. Add current user message
        /// </summary>
        public List<ChatMessage> BuildMessages(
            string systemPrompt,
            List<ClientMessageDTO>? clientHistory,
            string userMessage)
        {
            var messages = new List<ChatMessage>();

            // 1. System prompt (always included)
            var systemTokens = _tokenizer.CountTokens(systemPrompt);
            messages.Add(new SystemChatMessage(systemPrompt));

            // 2. User message (always included)
            var userTokens = _tokenizer.CountTokens(userMessage);

            // 3. Calculate available tokens for history
            var availableForHistory = HistoryTokenBudget - systemTokens - userTokens;

            _logger.LogDebug(
                "Context budget: System={SystemTokens}, User={UserTokens}, Available for history={Available}",
                systemTokens, userTokens, availableForHistory);

            // 4. Process history with sliding window + token budget
            if (clientHistory != null && clientHistory.Count > 0)
            {
                var selectedHistory = SelectHistoryMessages(clientHistory, availableForHistory);

                foreach (var msg in selectedHistory)
                {
                    if (msg.Role.ToLower() == "user")
                        messages.Add(new UserChatMessage(msg.Content));
                    else if (msg.Role.ToLower() == "assistant")
                        messages.Add(new AssistantChatMessage(msg.Content));
                }
            }

            // 5. Add current user message
            messages.Add(new UserChatMessage(userMessage));

            // Log final context size
            var totalTokens = systemTokens + userTokens +
                (clientHistory?.Sum(m => _tokenizer.CountTokens(m.Content)) ?? 0);
            _logger.LogInformation(
                "Built context: {MessageCount} messages, ~{TotalTokens} tokens",
                messages.Count, totalTokens);

            return messages;
        }

        /// <summary>
        /// Select history messages using sliding window + token budget
        ///
        /// Algorithm:
        /// 1. Take last MAX_HISTORY_MESSAGES (sliding window)
        /// 2. From newest to oldest, add messages until budget exhausted
        /// 3. Insert truncation notice if messages were dropped
        /// </summary>
        private List<ClientMessageDTO> SelectHistoryMessages(
            List<ClientMessageDTO> history,
            int tokenBudget)
        {
            var result = new List<ClientMessageDTO>();

            // Step 1: Apply sliding window
            var windowed = history
                .Where(m => !string.IsNullOrEmpty(m.Content))
                .TakeLast(MAX_HISTORY_MESSAGES)
                .ToList();

            // Step 2: Calculate tokens for each message
            var messagesWithTokens = windowed
                .Select(m => new
                {
                    Message = m,
                    Tokens = Math.Min(
                        _tokenizer.CountMessageTokens(m.Role, m.Content),
                        MAX_SINGLE_MESSAGE_TOKENS)
                })
                .ToList();

            // Step 3: Select from newest to oldest (reverse order)
            var usedTokens = 0;
            var selectedIndices = new List<int>();

            for (int i = messagesWithTokens.Count - 1; i >= 0; i--)
            {
                var item = messagesWithTokens[i];

                if (usedTokens + item.Tokens <= tokenBudget)
                {
                    selectedIndices.Insert(0, i); // Keep original order
                    usedTokens += item.Tokens;
                }
                else
                {
                    // Budget exhausted - remaining older messages will be dropped
                    var droppedCount = i + 1;
                    if (droppedCount > 0)
                    {
                        _logger.LogInformation(
                            "Context truncation: Dropped {DroppedCount} older messages, kept {KeptCount}",
                            droppedCount, selectedIndices.Count);

                        // Insert truncation notice at the beginning
                        result.Add(new ClientMessageDTO
                        {
                            Role = "system",
                            Content = $"[Lịch sử hội thoại đã được rút gọn. {droppedCount} tin nhắn cũ hơn đã bị lược bỏ để tiết kiệm context.]"
                        });
                    }
                    break;
                }
            }

            // Step 4: Add selected messages in original order
            foreach (var idx in selectedIndices)
            {
                var msg = messagesWithTokens[idx].Message;

                // Truncate individual message if too long
                var truncatedContent = msg.Content;
                if (_tokenizer.CountTokens(msg.Content) > MAX_SINGLE_MESSAGE_TOKENS)
                {
                    truncatedContent = _tokenizer.TruncateToTokenLimit(msg.Content, MAX_SINGLE_MESSAGE_TOKENS);
                }

                result.Add(new ClientMessageDTO
                {
                    Role = msg.Role,
                    Content = truncatedContent
                });
            }

            return result;
        }

        /// <summary>
        /// Kiểm tra xem context có đang gần đầy không
        /// Dùng để warning user hoặc trigger summarization
        /// </summary>
        public ContextStatus GetContextStatus(
            string systemPrompt,
            List<ClientMessageDTO>? history,
            string userMessage)
        {
            var systemTokens = _tokenizer.CountTokens(systemPrompt);
            var userTokens = _tokenizer.CountTokens(userMessage);
            var historyTokens = history?.Sum(m => _tokenizer.CountTokens(m.Content)) ?? 0;

            var totalUsed = systemTokens + userTokens + historyTokens;
            var totalBudget = MODEL_CONTEXT_WINDOW - MAX_OUTPUT_TOKENS;
            var usagePercent = (double)totalUsed / totalBudget * 100;

            return new ContextStatus
            {
                TotalTokensUsed = totalUsed,
                TotalBudget = totalBudget,
                UsagePercent = usagePercent,
                MessageCount = (history?.Count ?? 0) + 1,
                IsNearLimit = usagePercent > 80,
                IsCritical = usagePercent > 95
            };
        }

        /// <summary>
        /// Truncate tool result để tránh chiếm quá nhiều context
        /// </summary>
        public string TruncateToolResult(string result, int maxTokens = 8000)
        {
            return _tokenizer.TruncateToTokenLimit(result, maxTokens);
        }
    }

    /// <summary>
    /// Status của context window
    /// </summary>
    public class ContextStatus
    {
        public int TotalTokensUsed { get; set; }
        public int TotalBudget { get; set; }
        public double UsagePercent { get; set; }
        public int MessageCount { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsCritical { get; set; }
    }
}
