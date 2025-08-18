namespace SegurosApp.API.DTOs.Velneo
{
    public class VelneoIntegrationMetricsDto
    {
        public int TotalScans { get; set; }
        public int SuccessfulScans { get; set; }
        public int PendingVelneoCreation { get; set; }
        public int VelneoCreatedSuccessfully { get; set; }
        public int VelneoCreationFailed { get; set; }
        public decimal VelneoSuccessRate { get; set; }
        public decimal ProcessingEfficiency { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<DailyVelneoMetric> DailyMetrics { get; set; } = new();
        public List<ProblematicVelneoDocumentDto> ProblematicDocuments { get; set; } = new();
        public int AverageProcessingTimeMs { get; set; }
        public int AverageVelneoCreationTimeMs { get; set; }
        public QualityMetrics Quality { get; set; } = new();
    }
}
