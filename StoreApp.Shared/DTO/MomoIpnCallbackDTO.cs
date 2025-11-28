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
        //Mã đối tác MoMo
        public string? PartnerCode { get; set; }

        // Mã yêu cầu (requestId) - do bạn gửi lúc tạo đơn
        public string? RequestId { get; set; }

        // Mã đơn hàng của bạn
        public string? OrderId { get; set; }

        // Mã giao dịch MoMo
        public string? TransId { get; set; }

        // Số tiền thanh toán
        public string? Amount { get; set; }

        // Kết quả giao dịch (0 = thành công)
        public int? ResultCode { get; set; }

        // Thông báo kết quả
        public string? Message { get; set; }

        // Loại giao dịch (VD: momo_wallet)
        public string? OrderType { get; set; }

        // Thông tin đơn hàng (VD: Thanh toán đơn hàng #123)
        public string? OrderInfo { get; set; }

        // Hình thức thanh toán (VD: qr, napas, credit_card,...)
        public string? PayType { get; set; }

        // Thời điểm MoMo phản hồi (timestamp)
        public long? ResponseTime { get; set; }

        // Dữ liệu thêm bạn gửi kèm khi tạo đơn
        public string? ExtraData { get; set; }

        // Chữ ký để xác thực dữ liệu
        public string? Signature { get; set; }
    }
}
