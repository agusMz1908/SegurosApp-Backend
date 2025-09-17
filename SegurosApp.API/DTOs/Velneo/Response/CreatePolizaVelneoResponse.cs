using SegurosApp.API.DTOs.Velneo.Validation;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class CreatePolizaVelneoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public int ScanId { get; set; }
        public int? VelneoPolizaId { get; set; }
        public string? PolizaNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<string> Warnings { get; set; } = new();
        public PolizaValidationError? ValidationError { get; set; }
    }
}
