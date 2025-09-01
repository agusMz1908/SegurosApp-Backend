namespace SegurosApp.API.DTOs
{
    public class DocumentScanWithContextResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DocumentScanResponse ScanResult { get; set; } = new();
        public ValidatedPreSelection? PreSelection { get; set; }
        public PolizaMappingWithContextResponse PolizaMapping { get; set; } = new();
        public bool IsReadyForVelneo { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public CriticalFieldsStatus CriticalFields { get; set; } = new();
        public AzureModelUsageInfo? AzureModelInfo { get; set; }
    }

    public class AzureModelUsageInfo
    {
        public string ModelId { get; set; } = "";
        public int CompaniaId { get; set; }
        public string CompaniaName { get; set; } = "";
        public DateTime ProcessedAt { get; set; }
        public string? Description { get; set; }
    }
}