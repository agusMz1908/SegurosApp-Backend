namespace SegurosApp.API.DTOs
{
    public class TierUsageSummaryDto
    {
        public string TierName { get; set; } = string.Empty;
        public int CompaniesCount { get; set; }
        public int TotalPolizas { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PricePerPoliza { get; set; }
    }
}
