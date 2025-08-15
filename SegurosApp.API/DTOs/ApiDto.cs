namespace SegurosApp.API.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Para respuestas exitosas con datos
        public static ApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        // Para respuestas de error
        public static ApiResponse<T> ErrorResult(string errorMessage)
        {
            return new ApiResponse<T>
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    // Para respuestas simples sin datos, usa object como tipo
    public class ApiResponse : ApiResponse<object>
    {
        // Para respuestas exitosas sin datos
        public static ApiResponse SuccessResponse(string? message = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        // Para respuestas de error sin datos
        public static ApiResponse Error(string errorMessage)
        {
            return new ApiResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}