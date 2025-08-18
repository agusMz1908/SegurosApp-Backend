using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoCombustibleResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<CombustibleItem> combustibles { get; set; } = new();
    }
}
