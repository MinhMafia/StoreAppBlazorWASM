namespace StoreApp.Shared
{
    public class ServiceResultDTO<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; } = 200;

        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }

        public static ServiceResultDTO<T> CreateSuccessResult(T data, int statusCode = 200)
        {
            return new ServiceResultDTO<T>
            {
                Success = true,
                StatusCode = statusCode,
                Data = data
            };
        }

        public static ServiceResultDTO<T> CreateFailureResult(int statusCode, params string[] error_details)
        {
            return new ServiceResultDTO<T>
            {
                Success = false,
                StatusCode = statusCode,
                Errors = error_details.ToList()
            };
        }
    }
}