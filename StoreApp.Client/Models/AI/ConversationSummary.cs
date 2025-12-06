namespace StoreApp.Client.Models.AI
{
    /// <summary>
    /// Model cho conversation summary trong list
    /// </summary>
    public class ConversationSummary
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Model cho conversation detail với messages
    /// </summary>
    public class ConversationDetail
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ConversationMessage> Messages { get; set; } = new();
    }

    /// <summary>
    /// Model cho message trong conversation từ API
    /// </summary>
    public class ConversationMessage
    {
        public long Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? FunctionCalled { get; set; }
        public object? Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
