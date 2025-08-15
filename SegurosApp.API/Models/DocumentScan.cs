using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class DocumentScan
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required, MaxLength(50)]
        public string FileMd5Hash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? AzureOperationId { get; set; }

        public int ProcessingTimeMs { get; set; }
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int TotalFieldsAttempted { get; set; }

        [Required]
        public string ExtractedData { get; set; } = string.Empty; // JSON

        [Required, MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Processing, Completed, Error

        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [MaxLength(100)]
        public string? VelneoPolizaNumber { get; set; }

        public bool VelneoCreated { get; set; } = false;
        public string? VelneoResponse { get; set; }

        // Billing properties
        public bool IsBillable { get; set; } = true;
        public bool IsBilled { get; set; } = false;
        public DateTime? BilledAt { get; set; }
        public int? BillingItemId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public BillingItem? BillingItem { get; set; }
    }
}