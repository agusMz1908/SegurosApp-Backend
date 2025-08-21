using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class MonthlyBilling
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }

        public int TotalPolizasEscaneadas { get; set; }
        public int TotalBillableScans { get; set; }
        public int AppliedTierId { get; set; }
        public decimal PricePerPoliza { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; 

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }

        [MaxLength(100)]
        public string? PaymentMethod { get; set; }

        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [MaxLength(50)]
        public string? CompanyRUC { get; set; }

        public User User { get; set; } = null!;
        public PricingTier AppliedTier { get; set; } = null!;
        public List<BillingItem> BillingItems { get; set; } = new();
    }
}