namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class CreatePolizaVelneoResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int ScanId { get; set; }
        public int? VelneoPolizaId { get; set; }
        public string PolizaNumber { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public List<string> Warnings { get; set; } = new();
        public ValidationResult Validation { get; set; } = new();
        public string? VelneoUrl { get; set; }
        public string? PolizaViewUrl { get; set; }
    }
}
