namespace SegurosApp.API.DTOs
{
    public class BillingStatsDto
    {
        public int TotalPolizasThisMonth { get; set; }
        public decimal EstimatedCost { get; set; }
        public string ApplicableTierName { get; set; } = string.Empty;
        public decimal PricePerPoliza { get; set; }
        public int DaysLeftInMonth { get; set; }
        public DateTime LastBillingDate { get; set; }
    }
}