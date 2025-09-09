namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class ModifyPolizaResponse : CreatePolizaVelneoResponse
    {
        public int? PolizaAnteriorId { get; set; }
        public string? TipoCambio { get; set; }
        public bool PolizaAnteriorActualizada { get; set; }
        public string? MensajePolizaAnterior { get; set; }
    }

    public class UpdatePolizaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? PolizaId { get; set; }
        public string? UpdatedFields { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}