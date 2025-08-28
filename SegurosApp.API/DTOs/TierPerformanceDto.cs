namespace SegurosApp.API.DTOs
{
    public class TierPerformanceDto
    {
        public string TierName { get; set; } = string.Empty;
        public decimal PricePerPoliza { get; set; }
        public int TotalUsage { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenuePercentage { get; set; }
        public int MinPolizas { get; set; }
        public int MaxPolizas { get; set; }
    }
}
