namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoOperationStatsDto
    {
        public int Successful { get; set; }
        public int Failed { get; set; }
        public int Total { get; set; }
        public decimal SuccessRate { get; set; }
        public double AverageDurationMs { get; set; }
    }
}
