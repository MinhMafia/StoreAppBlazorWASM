using Microsoft.ML.Tokenizers;

namespace StoreApp.Services
{
    /// <summary>
    /// Service đếm token sử dụng Tiktoken (chuẩn OpenAI)
    /// </summary>
    public class TokenizerService
    {
        private readonly Tokenizer _tokenizer;

        public TokenizerService()
        {
            _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
        }

        /// <summary>
        /// Đếm số token trong text
        /// </summary>
        public int CountTokens(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return _tokenizer.CountTokens(text);
        }

        /// <summary>
        /// Đếm token cho một message (bao gồm overhead của role)
        /// </summary>
        public int CountMessageTokens(string role, string content)
        {
            const int MESSAGE_OVERHEAD = 4;
            return CountTokens(content) + MESSAGE_OVERHEAD;
        }

        /// <summary>
        /// Đếm tổng token cho danh sách messages
        /// </summary>
        public int CountMessagesTokens(IEnumerable<(string role, string content)> messages)
        {
            const int CONVERSATION_OVERHEAD = 3;
            return CONVERSATION_OVERHEAD + messages.Sum(m => CountMessageTokens(m.role, m.content));
        }

        /// <summary>
        /// Ước tính token cho function/tool definitions (~200 tokens/function)
        /// </summary>
        public int EstimateFunctionTokens(int functionCount)
        {
            return functionCount * 200;
        }

      
        public string TruncateToTokenLimit(string text, int maxTokens)
        {
            if (string.IsNullOrEmpty(text) || CountTokens(text) <= maxTokens)
                return text;

            // Ước lượng tỷ lệ ký tự/token rồi cắt
            double ratio = (double)text.Length / CountTokens(text);
            int estimatedLength = (int)(maxTokens * ratio * 0.9); // 90% để an toàn

            var result = text[..Math.Min(estimatedLength, text.Length)];

            // Cắt ở ranh giới từ nếu có thể
            int lastSpace = result.LastIndexOf(' ');
            if (lastSpace > result.Length * 0.8)
                result = result[..lastSpace];

            return result + "...";
        }
    }
}
