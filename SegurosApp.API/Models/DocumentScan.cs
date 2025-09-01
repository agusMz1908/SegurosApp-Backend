using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SegurosApp.API.Models
{
    [Table("DocumentScans")]
    public class DocumentScan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(32)]
        public string FileMd5Hash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AzureOperationId { get; set; }
        [MaxLength(100)]
        public string? AzureModelId { get; set; }

        public int ProcessingTimeMs { get; set; }
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int TotalFieldsAttempted { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public string ExtractedData { get; set; } = "{}";

        public bool IsDuplicate { get; set; }
        public int? ExistingScanId { get; set; }

        [Column("cliente_id")]
        public int? ClienteId { get; set; }

        [Column("compania_id")]
        public int? CompaniaId { get; set; }

        [Column("seccion_id")]
        public int? SeccionId { get; set; }

        [MaxLength(500)]
        [Column("preselection_notes")]
        public string? PreSelectionNotes { get; set; }

        [Column("preselection_saved_at")]
        public DateTime? PreSelectionSavedAt { get; set; }

        [MaxLength(100)]
        public string? VelneoPolizaNumber { get; set; }
        public bool VelneoCreated { get; set; }

        public bool IsBillable { get; set; } = true;
        public bool IsBilled { get; set; }
        public DateTime? BilledAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
