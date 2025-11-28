namespace StoreApp.Shared
{
    public class ImportErrorDTO
    {
        public int RowNumber { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RowData { get; set; }
    }
}

