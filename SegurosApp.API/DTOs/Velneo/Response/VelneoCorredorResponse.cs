using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoCorredorResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<CorredorItem> corredores { get; set; } = new();
    }
}
