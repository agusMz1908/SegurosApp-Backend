using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class PricingTier
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string TierName { get; set; } = string.Empty;
        public int MinPolizas { get; set; }
        public int? MaxPolizas { get; set; }
        public decimal PricePerPoliza { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<MonthlyBilling> MonthlyBillings { get; set; } = new();
    }
}