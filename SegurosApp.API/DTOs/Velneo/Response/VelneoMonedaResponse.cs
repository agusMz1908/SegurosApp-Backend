using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoMonedaResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<MonedaItem> monedas { get; set; } = new();
    }
}
