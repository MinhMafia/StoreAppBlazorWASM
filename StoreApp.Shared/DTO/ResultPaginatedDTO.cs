namespace StoreApp.Shared
{
    public class ResultPaginatedDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
