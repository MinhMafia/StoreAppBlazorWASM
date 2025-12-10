using System;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    /*
        PartnerCode:Mã đối tác MoMo
        RequestId:Mã yêu cầu (requestId) - do bạn gửi lúc tạo đơn
        OrderId:Mã đơn hàng của bạn
        TransId:Mã giao dịch MoMo
        Amount:Số tiền thanh toán
        ResultCode:Kết quả giao dịch (0 = thành công)
        Message:Thông báo kết quả
        OrderType:Loại giao dịch (VD: momo_wallet)
        OrderInfo:Thông tin đơn hàng (VD: Thanh toán đơn hàng #123)
        PayType:Hình thức thanh toán (VD: qr, napas, credit_card,...)
        ResponseTime:Thời điểm MoMo phản hồi (timestamp)
        ExtraData:Dữ liệu thêm bạn gửi kèm khi tạo đơn
        Signature:Chữ ký để xác thực dữ liệu
    */
    public class MomoIpnCallbackDTO
        {
        [JsonPropertyName("partnerCode")]
        public string? PartnerCode { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("transId")]
        public long? TransId { get; set; }

        [JsonPropertyName("amount")]
        public long? Amount { get; set; }

        [JsonPropertyName("resultCode")]
        public int? ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("orderType")]
        public string? OrderType { get; set; }

        [JsonPropertyName("orderInfo")]
        public string? OrderInfo { get; set; }

        [JsonPropertyName("payType")]
        public string? PayType { get; set; }

        [JsonPropertyName("responseTime")]
        public long? ResponseTime { get; set; }

        [JsonPropertyName("extraData")]
        public string? ExtraData { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }
        }
}
