public class DashboardDto
{
    public int TotalScansThisMonth { get; set; }
    public int BillableScansThisMonth { get; set; }
    public decimal SuccessRateThisMonth { get; set; }
    public decimal EstimatedCostThisMonth { get; set; }
    public PricingTierDto? CurrentTier { get; set; }
    public PricingTierDto? NextTier { get; set; }
    public int PolizasToNextTier { get; set; }
    public List<RecentScanDto> RecentScans { get; set; } = new();
}

public class RecentScanDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SuccessRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VelneoPolizaNumber { get; set; }
    public bool VelneoCreated { get; set; }
}