namespace SegurosApp.API.DTOs
{
    public class PreSelectionContext
    {
        public int ClienteId { get; set; }
        public int CompaniaId { get; set; }
        public int SeccionId { get; set; }
        public int ScanId { get; set; }
        public int UserId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
