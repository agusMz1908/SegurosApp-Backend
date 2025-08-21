using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class BillingItems
    {
        public int Id { get; set; }
        public int MonthlyBillingId { get; set; }
        public int DocumentScanId { get; set; }

        public DateTime ScanDate { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? VelneoPolizaNumber { get; set; }

        public decimal PricePerPoliza { get; set; }
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MonthlyBilling MonthlyBilling { get; set; } = null!;
        public DocumentScan DocumentScan { get; set; } = null!;
    }
}