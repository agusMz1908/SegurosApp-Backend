using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SegurosApp.API.Models
{
    [Table("TenantConfigurations")]
    public class TenantConfiguration
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        [MaxLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string VelneoApiKey { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? VelneoBaseUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CustomSettings { get; set; } = "{}";

        public string? ClientNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}