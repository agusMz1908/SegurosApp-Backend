using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class CreatePolizaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? VelneoPolizaId { get; set; }
        public int? PolizaId { get; set; }
        public string PolizaNumber { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public string VelneoUrl { get; set; } = ""; 
        public List<string> Warnings { get; set; } = new();
        public ValidationResult Validation { get; set; } = new();
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> FieldsValidated { get; set; } = new();
    }
}
