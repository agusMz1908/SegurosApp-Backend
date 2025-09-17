namespace SegurosApp.API.DTOs.Velneo.Validation
{
    public class PolizaExistsValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string? CompaniaName { get; set; }
        public int? ExistingPolizaId { get; set; }
        public string? ExistingPolizaStatus { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
        public ExistingPolizaInfo? ExistingPolizaDetails { get; set; }
    }
}
