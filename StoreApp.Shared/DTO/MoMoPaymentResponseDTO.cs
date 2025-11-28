using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class MomoPaymentResponseDTO
    {
        /*
           DTO/MomoPaymentResponseDTO.cs => trả về cho client sau khi gọi API tạo payment ở MoMo.
           Chứa các thông tin như:
           - PayUrl: đường dẫn redirect tới cổng MoMo để user thực hiện thanh toán.
           - RequestId/OrderId: id do hệ thống MoMo trả về (dùng để debug/đối chiếu).
           - Message/Success: thông báo trạng thái.
        */
        public string PayUrl { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;

        public string? Message { get; set; }

        public bool Success { get; set; }

    }
}