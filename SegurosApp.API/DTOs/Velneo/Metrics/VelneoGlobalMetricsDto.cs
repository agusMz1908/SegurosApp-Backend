namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoGlobalMetricsDto
    {
        public int TotalSuccessful { get; set; }
        public int TotalFailed { get; set; }
        public int TotalOperations { get; set; }
        public decimal SuccessRate { get; set; }
        public double AverageDurationMs { get; set; }
        public decimal OperationsPerDay { get; set; }
    }
}
