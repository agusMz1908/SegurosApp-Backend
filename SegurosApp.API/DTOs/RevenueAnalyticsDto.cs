namespace SegurosApp.API.DTOs
{
    public class RevenueAnalyticsDto
    {
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public RevenueMetricsDto TotalMetrics { get; set; } = new();
        public List<TierPerformanceDto> TierPerformance { get; set; } = new();
        public GrowthAnalysisDto GrowthAnalysis { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
