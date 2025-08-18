namespace SegurosApp.API.DTOs.Velneo
{
    public class ProblematicVelneoDocumentDto
    {
        public int ScanId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime? LastRetryAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasClienteId { get; set; }
        public bool HasCompaniaId { get; set; }
        public bool HasSeccionId { get; set; }
        public bool HasPolicyNumber { get; set; }
        public decimal DataCompleteness { get; set; }
    }
}
