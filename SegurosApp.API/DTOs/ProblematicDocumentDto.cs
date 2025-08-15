namespace SegurosApp.API.DTOs
{
    public class ProblematicDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public decimal AverageSuccessRate { get; set; }
        public int ErrorCount { get; set; }
        public string? LastError { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}
