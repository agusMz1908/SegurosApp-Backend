using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoContratoResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<ContratoItem> contratos { get; set; } = new List<ContratoItem>();
        public string? status { get; set; }
        public string? message { get; set; }
        public DateTime? timestamp { get; set; }
        public int? current_page { get; set; }
        public int? per_page { get; set; }
        public int? total_pages { get; set; }
        public bool HasData => contratos?.Count > 0;
        public bool IsEmpty => contratos?.Count == 0;
        public double ResponseTime { get; set; } 
    }
}