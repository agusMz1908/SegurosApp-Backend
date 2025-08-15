public class PricingTierDto
{
    public int Id { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int MinPolizas { get; set; }
    public int? MaxPolizas { get; set; }
    public decimal PricePerPoliza { get; set; }
    public bool IsActive { get; set; }
    public string RangeDescription => MaxPolizas == null
        ? $"{MinPolizas}+ pólizas"
        : $"{MinPolizas} - {MaxPolizas} pólizas";
}