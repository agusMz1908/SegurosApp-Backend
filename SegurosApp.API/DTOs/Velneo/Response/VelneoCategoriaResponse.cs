using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoCategoriaResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<CategoriaItem> categorias { get; set; } = new();
    }
}
