using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoSeccionResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<SeccionItem> secciones { get; set; } = new();
    }
}
