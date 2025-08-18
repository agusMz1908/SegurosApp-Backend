using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoDestinoResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<DestinoItem> destinos { get; set; } = new();
    }
}
