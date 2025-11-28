
using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class CategoryDTO
    {
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
