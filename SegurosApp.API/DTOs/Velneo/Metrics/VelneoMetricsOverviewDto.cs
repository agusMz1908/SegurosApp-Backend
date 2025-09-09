namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoMetricsOverviewDto
    {
        public VelneoGlobalMetricsDto Global { get; set; } = new();
        public VelneoOperationMetricsDto ByOperation { get; set; } = new();
        public VelneoPeriodMetricsDto ByPeriod { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
