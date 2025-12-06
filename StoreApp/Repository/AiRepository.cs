using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class AiRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AiRepository> _logger;

        public AiRepository(AppDbContext context, ILogger<AiRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Conversations

        /// <summary>
        /// Lấy danh sách conversations của user, sắp xếp theo thời gian cập nhật
        /// </summary>
        public async Task<List<AiConversation>> GetConversationsByUserIdAsync(int userId, int limit = 50)
        {
            try
            {
                return await _context.AiConversations
                    .AsNoTracking()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Lấy conversation với messages, validate ownership
        /// </summary>
        public async Task<AiConversation?> GetConversationByIdAsync(int id, int userId)
        {
            try
            {
                return await _context.AiConversations
                    .AsNoTracking()
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId} for user {UserId}", id, userId);
                throw;
            }
        }

        /// <summary>
        /// Tạo conversation mới
        /// </summary>
        public async Task<AiConversation> CreateConversationAsync(int userId, string? title = null)
        {
            try
            {
                var conversation = new AiConversation
                {
                    UserId = userId,
                    Title = SanitizeTitle(title),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AiConversations.Add(conversation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created conversation {ConversationId} for user {UserId}", conversation.Id, userId);
                return conversation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Cập nhật title conversation
        /// </summary>
        public async Task UpdateConversationTitleAsync(int id, int userId, string title)
        {
            try
            {
                var conversation = await _context.AiConversations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation != null)
                {
                    conversation.Title = SanitizeTitle(title);
                    conversation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation {ConversationId} title", id);
                throw;
            }
        }

        /// <summary>
        /// Xóa conversation và tất cả messages (cascade delete)
        /// </summary>
        public async Task DeleteConversationAsync(int id, int userId)
        {
            try
            {
                var conversation = await _context.AiConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (conversation != null)
                {
                    _context.AiConversations.Remove(conversation);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted conversation {ConversationId} for user {UserId}", id, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra conversation có thuộc về user không
        /// </summary>
        public async Task<bool> IsConversationOwnedByUserAsync(int conversationId, int userId)
        {
            try
            {
                return await _context.AiConversations
                    .AnyAsync(c => c.Id == conversationId && c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conversation ownership");
                return false;
            }
        }

        #endregion

        #region Messages

        /// <summary>
        /// Thêm message vào conversation
        /// </summary>
        public async Task<AiMessage> AddMessageAsync(
            int conversationId, 
            string role, 
            string content, 
            string? functionCalled = null, 
            string? functionData = null)
        {
            try
            {
                // Validate role
                if (role != "user" && role != "assistant" && role != "system")
                {
                    throw new ArgumentException($"Invalid role: {role}");
                }

                var message = new AiMessage
                {
                    ConversationId = conversationId,
                    Role = role,
                    Content = SanitizeContent(content),
                    FunctionCalled = functionCalled,
                    FunctionData = functionData,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AiMessages.Add(message);

                // Update conversation's UpdatedAt
                var conversation = await _context.AiConversations.FindAsync(conversationId);
                if (conversation != null)
                {
                    conversation.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogDebug("Added {Role} message to conversation {ConversationId}", role, conversationId);
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message to conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Thêm nhiều messages cùng lúc (batch insert)
        /// </summary>
        public async Task AddMessagesAsync(int conversationId, IEnumerable<(string role, string content)> messages)
        {
            try
            {
                var now = DateTime.UtcNow;
                var messageEntities = messages.Select(m => new AiMessage
                {
                    ConversationId = conversationId,
                    Role = m.role,
                    Content = SanitizeContent(m.content),
                    CreatedAt = now
                });

                _context.AiMessages.AddRange(messageEntities);

                var conversation = await _context.AiConversations.FindAsync(conversationId);
                if (conversation != null)
                {
                    conversation.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch adding messages to conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Lấy messages của conversation với giới hạn
        /// </summary>
        public async Task<List<AiMessage>> GetMessagesByConversationIdAsync(int conversationId, int limit = 50)
        {
            try
            {
                return await _context.AiMessages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Lấy N messages gần nhất
        /// </summary>
        public async Task<List<AiMessage>> GetRecentMessagesAsync(int conversationId, int count = 20)
        {
            try
            {
                var messages = await _context.AiMessages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                // Đảo ngược để có thứ tự thời gian đúng
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent messages for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Đếm số messages trong conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            try
            {
                return await _context.AiMessages
                    .CountAsync(m => m.ConversationId == conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting messages for conversation {ConversationId}", conversationId);
                return 0;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Xóa conversations cũ hơn N ngày
        /// </summary>
        public async Task<int> CleanupOldConversationsAsync(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

                var oldConversations = await _context.AiConversations
                    .Include(c => c.Messages)
                    .Where(c => c.UpdatedAt < cutoffDate)
                    .ToListAsync();

                if (oldConversations.Any())
                {
                    _context.AiConversations.RemoveRange(oldConversations);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old conversations", oldConversations.Count);
                }

                return oldConversations.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old conversations");
                return 0;
            }
        }

        /// <summary>
        /// Lấy thống kê AI usage cho admin
        /// </summary>
        public async Task<AiUsageStats> GetUsageStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                var conversations = await _context.AiConversations
                    .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
                    .ToListAsync();

                var messages = await _context.AiMessages
                    .Where(m => m.CreatedAt >= fromDate && m.CreatedAt <= toDate)
                    .ToListAsync();

                return new AiUsageStats
                {
                    TotalConversations = conversations.Count,
                    TotalMessages = messages.Count,
                    UniqueUsers = conversations.Select(c => c.UserId).Distinct().Count(),
                    UserMessages = messages.Count(m => m.Role == "user"),
                    AssistantMessages = messages.Count(m => m.Role == "assistant"),
                    AverageMessagesPerConversation = conversations.Count > 0
                        ? (double)messages.Count / conversations.Count
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI usage stats");
                return new AiUsageStats();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Sanitize title để tránh XSS và giới hạn độ dài
        /// </summary>
        private static string? SanitizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            // Remove potential XSS
            title = System.Text.RegularExpressions.Regex.Replace(title, @"<[^>]*>", "");
            // Remove control characters
            title = System.Text.RegularExpressions.Regex.Replace(title, @"[\x00-\x1F\x7F]", "");
            // Limit length
            if (title.Length > 255)
            {
                title = title[..252] + "...";
            }

            return title.Trim();
        }

        /// <summary>
        /// Sanitize content
        /// </summary>
        private static string SanitizeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";

            // Remove null characters
            content = content.Replace("\0", "");

            // Limit extremely long content
            if (content.Length > 50000)
            {
                content = content[..50000] + "\n[Nội dung đã bị cắt do quá dài]";
            }

            return content;
        }

        #endregion
    }

    #region DTOs

    public class AiUsageStats
    {
        public int TotalConversations { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueUsers { get; set; }
        public int UserMessages { get; set; }
        public int AssistantMessages { get; set; }
        public double AverageMessagesPerConversation { get; set; }
    }

    #endregion
}
