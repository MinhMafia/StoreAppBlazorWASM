namespace  StoreApp.Shared
{
    public class OrderItemReponse
    {
        public int id { get; set; }
        public string product { get; set; } = string.Empty;
        public int qty { get; set; }
        public decimal price { get; set; }
        public decimal total { get; set; }
    }
}
