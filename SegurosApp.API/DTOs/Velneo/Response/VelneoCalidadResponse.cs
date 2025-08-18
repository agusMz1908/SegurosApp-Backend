using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoCalidadResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<CalidadItem> calidades { get; set; } = new();
    }

}
