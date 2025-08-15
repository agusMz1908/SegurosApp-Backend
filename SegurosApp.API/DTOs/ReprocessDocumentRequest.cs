using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs
{
    public class ReprocessDocumentRequest
    {
        [Required]
        public int ScanId { get; set; }

        public bool ForceReprocess { get; set; } = false;
        public string? Notes { get; set; }
    }
}
