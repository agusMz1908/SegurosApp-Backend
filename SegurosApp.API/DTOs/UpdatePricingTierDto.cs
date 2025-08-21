using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs
{
    public class UpdatePricingTierDto
    {
        [Required, MaxLength(100)]
        public string TierName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "MinPolizas debe ser mayor a 0")]
        public int MinPolizas { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MaxPolizas debe ser mayor a 0")]
        public int? MaxPolizas { get; set; }

        [Range(0.01, 10000, ErrorMessage = "PricePerPoliza debe estar entre 0.01 y 10000")]
        public decimal PricePerPoliza { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MaxPolizas.HasValue && MaxPolizas <= MinPolizas)
            {
                yield return new ValidationResult(
                    "MaxPolizas debe ser mayor que MinPolizas",
                    new[] { nameof(MaxPolizas) });
            }
        }
    }
}
