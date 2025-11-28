using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class ActivityLogCreateDTO
    {
        /*
            DTO/ActivityLogCreateDTO.cs => DTO để ghi log hoạt động người dùng.
            Chứa các thông tin như:
            - UserId: id người dùng thực hiện hành động.
            - Username: tên đăng nhập người dùng.
            - Action: mô tả hành động đã thực hiện.
            - EntityType/EntityId: đối tượng liên quan (Order, Payment, Product...).
            - Payload: nội dung chi tiết (JSON string)
            - IpAddress: ip client
           
        */
        public string? Username { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }


    }
}