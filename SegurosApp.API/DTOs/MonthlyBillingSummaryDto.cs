namespace SegurosApp.API.DTOs
{
    public class MonthlyBillingSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalCompanies { get; set; }
        public int TotalPolizasEscaneadas { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerCompany { get; set; }
        public decimal AveragePolizasPerCompany { get; set; }
        public List<TierUsageSummaryDto> TierUsage { get; set; } = new();
        public PaymentStatusSummaryDto PaymentStatus { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
