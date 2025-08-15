namespace SegurosApp.API.DTOs
{
    public class AzureFieldResult
    {
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public decimal Confidence { get; set; }
        public bool IsExtracted { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
