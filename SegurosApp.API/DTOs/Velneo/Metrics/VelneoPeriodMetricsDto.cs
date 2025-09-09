namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoPeriodMetricsDto
    {
        public VelneoOperationStatsDto Today { get; set; } = new();
        public VelneoOperationStatsDto ThisWeek { get; set; } = new();
        public VelneoOperationStatsDto ThisMonth { get; set; } = new();
        public VelneoOperationStatsDto Last24Hours { get; set; } = new();
        public VelneoOperationStatsDto Last7Days { get; set; } = new();
        public VelneoOperationStatsDto Last30Days { get; set; } = new();
    }
}
