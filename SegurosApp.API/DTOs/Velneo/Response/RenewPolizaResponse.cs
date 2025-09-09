namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class RenewPolizaResponse : CreatePolizaVelneoResponse
    {
        public int? PolizaAnteriorId { get; set; }
        public DateTime? FechaVencimientoAnterior { get; set; }
        public bool PolizaAnteriorActualizada { get; set; }
        public string? MensajePolizaAnterior { get; set; }
        public bool VencimientoValidado { get; set; }
    }
}