namespace StoreApp.Shared
{
    public class PaymentResponseDTO
    {
        public PaymentResponseDTO(string method, string? transaction_ref, string status)
        {
            this.method = method;
            this.transaction_ref = transaction_ref;
            this.status = status;
        }

        //method
        public string method {get;set;}

        //transaction_ref
        public string? transaction_ref { get; set; }

        //status
        public string status {get;set;}
  
    }
}