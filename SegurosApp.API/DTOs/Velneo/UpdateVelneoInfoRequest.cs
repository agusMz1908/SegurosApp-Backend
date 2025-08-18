namespace SegurosApp.API.DTOs.Velneo
{
    public class UpdateVelneoInfoRequest
    {
        public int ScanId { get; set; }
        public string? VelneoPolizaNumber { get; set; }
        public bool VelneoCreated { get; set; }
        public string? VelneoErrorMessage { get; set; }
        public DateTime? VelneoCreatedAt { get; set; }
        public string? VelneoUrl { get; set; }
        public string? Notes { get; set; }
    }
}
