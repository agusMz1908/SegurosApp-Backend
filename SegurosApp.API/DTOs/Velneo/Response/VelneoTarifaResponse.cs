using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoTarifaResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<TarifaItem> tarifas { get; set; } = new();
    }
}
