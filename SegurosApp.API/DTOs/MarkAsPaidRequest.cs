using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs
{
    public class MarkAsPaidRequest
    {
        [Required, MaxLength(100)]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? PaymentReference { get; set; }
    }
}
