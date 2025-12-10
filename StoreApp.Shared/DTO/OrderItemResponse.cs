namespace  StoreApp.Shared
{
    public class OrderItemReponse
    {
        // MÃ SẢN PHẨM 
        public int id { get; set; }
        public int orderid { get; set; }
        public string product { get; set; } = string.Empty;
        public int qty { get; set; }
        public decimal price { get; set; }
        public decimal total { get; set; }


        // Thêm các thuộc tính khác nếu cần
        public int inventoryQuantity { get; set; }
    }
}
