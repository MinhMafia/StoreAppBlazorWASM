using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class MomoPaymentRequestDTO
    {
        /*
            DTO/MomoPaymentRequestDTO.cs => dùng để client gửi lên khi muốn khởi tạo thanh toán MoMo.
            Chứa các thông tin cần thiết như:
            - OrderId: Mã đơn hàng nội bộ của hệ thống.
            - Amount: Số tiền cần thanh toán.
            - ReturnUrl: URL MoMo redirect user sau khi thanh toán (frontend).
            - NotifyUrl: URL MoMo gửi IPN (server-to-server callback) để StoreApp cập nhật trạng thái.
        */

        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string ReturnUrl { get; set; } = string.Empty;

        public string NotifyUrl { get; set; } = string.Empty;

    }
}