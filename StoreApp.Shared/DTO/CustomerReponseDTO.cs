namespace StoreApp.Shared
{
    public class CustomerResponseDTO
    {
        public int Id { get; set; }
        public int? UserId { get; set; } = null;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Address { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}