namespace SegurosApp.API.DTOs
{
    public class RevenueMetricsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
        public decimal HighestMonthRevenue { get; set; }
        public decimal LowestMonthRevenue { get; set; }
        public int TotalCompaniesServed { get; set; }
        public int TotalPolizasProcessed { get; set; }
    }
}
