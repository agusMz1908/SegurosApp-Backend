namespace SegurosApp.API.DTOs.Velneo.Validation
{
    public class PolizaValidationError
    {
        public string Type { get; set; } = "";
        public string NumeroPoliza { get; set; } = "";
        public int CompaniaId { get; set; }
        public string CompaniaName { get; set; } = "";
        public int? ExistingPolizaId { get; set; }
        public string? ExistingPolizaStatus { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
        public ExistingPolizaInfo? ExistingPolizaInfo { get; set; }
    }
}
