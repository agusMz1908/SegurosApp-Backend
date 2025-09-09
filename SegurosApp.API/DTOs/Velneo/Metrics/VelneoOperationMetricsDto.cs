namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoOperationMetricsDto
    {
        public VelneoOperationStatsDto Create { get; set; } = new();
        public VelneoOperationStatsDto Modify { get; set; } = new();
        public VelneoOperationStatsDto Renew { get; set; } = new();
    }
}
