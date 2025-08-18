using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoDepartamentoResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<DepartamentoItem> departamentos { get; set; } = new();
    }
}
