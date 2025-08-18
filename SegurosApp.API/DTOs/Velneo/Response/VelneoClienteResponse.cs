using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoClienteDetalleResponse
    {
        public int count { get; set; }
        public int total_count { get; set; }
        public List<ClienteItem> clientes { get; set; } = new();
    }
}
