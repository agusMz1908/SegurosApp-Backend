namespace SegurosApp.API.DTOs
{
    public class DocumentMetricsDto
    {
        public int TotalScans { get; set; }
        public int SuccessfulScans { get; set; }
        public int FailedScans { get; set; }
        public int BillableScans { get; set; }
        public decimal AverageSuccessRate { get; set; }
        public int AverageProcessingTimeMs { get; set; }

        // Por período
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        // Top archivos problemáticos
        public List<ProblematicDocumentDto> ProblematicDocuments { get; set; } = new();
    }
}
