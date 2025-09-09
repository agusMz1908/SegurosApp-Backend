namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class RenewPolizaRequest
    {
        public int PolizaAnteriorId { get; set; }
        public string? Observaciones { get; set; }
        public bool ValidarVencimiento { get; set; } = true;
        public int DiasAntesVencimiento { get; set; } = 30;
    }
}