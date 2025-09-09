namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class ModifyPolizaRequest
    {
        public int PolizaAnteriorId { get; set; }
        public string TipoCambio { get; set; } = "";
        public string? Observaciones { get; set; }
    }
}