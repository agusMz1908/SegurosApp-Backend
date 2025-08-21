using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs
{
    public class GenerateBillRequest
    {
        [Range(2020, 2030, ErrorMessage = "Año debe estar entre 2020 y 2030")]
        public int Year { get; set; }

        [Range(1, 12, ErrorMessage = "Mes debe estar entre 1 y 12")]
        public int Month { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [MaxLength(50)]
        public string? CompanyRUC { get; set; }
    }
}