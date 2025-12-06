using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class UnitDTO
    {
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Thống kê (optional, có thể tính từ query)
        [JsonPropertyName("productCount")]
        public int ProductCount { get; set; } = 0;
    }
}