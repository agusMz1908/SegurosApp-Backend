namespace SegurosApp.API.DTOs
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse SuccessResult(string message = "Operación exitosa")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse ErrorResult(string message = "Error en la operación")
        {
            return new ApiResponse
            {
                Success = false,
                Message = message
            };
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string message = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static new ApiResponse<T> ErrorResult(string message = "Error en la operación")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default(T)
            };
        }
    }
}