using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoCompaniaResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<CompaniaItem> companias { get; set; } = new();
    }
}
