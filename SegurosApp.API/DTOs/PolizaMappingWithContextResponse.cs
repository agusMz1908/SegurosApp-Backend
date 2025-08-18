using SegurosApp.API.DTOs.SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Response;

namespace SegurosApp.API.DTOs
{
    public class PolizaMappingWithContextResponse
    {
        public bool IsComplete { get; set; } = false;
        public decimal CompletionPercentage { get; set; } = 0;
        public decimal OverallCompletionPercentage { get; set; } = 0;
        public PolizaDataMapped MappedData { get; set; } = new();
        public List<FieldMappingIssue> RequiresAttention { get; set; } = new();
        public List<FieldSuggestion> AutoSuggestions { get; set; } = new();
        public List<string> ConfirmedByPreSelection { get; set; } = new();
        public MappingMetrics MappingMetrics { get; set; } = new();
    }
}
