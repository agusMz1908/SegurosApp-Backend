namespace SegurosApp.API.DTOs
{
    public class AzureDocumentResult
    {
        public string AzureOperationId { get; set; } = string.Empty;
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int TotalFieldsAttempted { get; set; }
        public Dictionary<string, object> ExtractedFields { get; set; } = new();
        public List<AzureFieldResult> FieldResults { get; set; } = new();
    }
}
