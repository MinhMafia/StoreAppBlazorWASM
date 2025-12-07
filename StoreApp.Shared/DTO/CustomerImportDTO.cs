namespace StoreApp.Shared
{
    public class CustomerImportDTO
    {
        public int? UserId { get; set; } = null;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
    }
}

