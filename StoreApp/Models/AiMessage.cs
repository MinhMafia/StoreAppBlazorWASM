using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreApp.Models
{
    [Table("ai_messages")]
    public class AiMessage
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("role")]
        public string Role { get; set; } = string.Empty; // "user", "assistant", "system"

        [Required]
        [Column("content", TypeName = "text")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("function_called")]
        public string? FunctionCalled { get; set; }

        [Column("function_data", TypeName = "json")]
        public string? FunctionData { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey(nameof(ConversationId))]
        public virtual AiConversation? Conversation { get; set; }
    }
}
