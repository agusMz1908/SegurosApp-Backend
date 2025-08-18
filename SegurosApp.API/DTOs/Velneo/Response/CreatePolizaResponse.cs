namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class CreatePolizaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PolizaId { get; set; }
        public string? PolizaNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
