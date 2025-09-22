namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class RenewPolizaApiResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public int? velneoPolizaId { get; set; }
        public string? polizaNumber { get; set; }
        public string? errorMessage { get; set; }
        public int? scanId { get; set; }
        public int? polizaAnteriorId { get; set; }
        public DateTime? fechaVencimientoAnterior { get; set; }
        public bool polizaAnteriorActualizada { get; set; }
        public string? mensajePolizaAnterior { get; set; }
        public bool vencimientoValidado { get; set; }
        public string? validationError { get; set; }
        public List<string> warnings { get; set; } = new();
        public DateTime? createdAt { get; set; }
        public Dictionary<string, object>? debugInfo { get; set; }
        public long? processingTimeMs { get; set; }

        public static RenewPolizaApiResponse Success(
            int velneoPolizaId,
            string polizaNumber,
            int polizaAnteriorId,
            string message = "Renovación procesada exitosamente")
        {
            return new RenewPolizaApiResponse
            {
                success = true,
                message = message,
                velneoPolizaId = velneoPolizaId,
                polizaNumber = polizaNumber,
                polizaAnteriorId = polizaAnteriorId,
                polizaAnteriorActualizada = true,
                vencimientoValidado = true,
                createdAt = DateTime.UtcNow
            };
        }

        public static RenewPolizaApiResponse Error(
            string message,
            string? errorDetails = null,
            int? polizaAnteriorId = null,
            int? scanId = null)
        {
            return new RenewPolizaApiResponse
            {
                success = false,
                message = message,
                errorMessage = errorDetails,
                polizaAnteriorId = polizaAnteriorId,
                scanId = scanId,
                polizaAnteriorActualizada = false,
                vencimientoValidado = false,
                createdAt = DateTime.UtcNow
            };
        }

        public static RenewPolizaApiResponse FromVelneoResponse(
            RenewPolizaResponse velneoResponse,
            int scanId,
            long processingTimeMs = 0)
        {
            return new RenewPolizaApiResponse
            {
                success = velneoResponse.Success,
                message = velneoResponse.Message ?? "",
                velneoPolizaId = velneoResponse.VelneoPolizaId,
                polizaNumber = velneoResponse.PolizaNumber,
                errorMessage = velneoResponse.ErrorMessage,
                scanId = scanId,
                polizaAnteriorId = velneoResponse.PolizaAnteriorId,
                fechaVencimientoAnterior = velneoResponse.FechaVencimientoAnterior,
                polizaAnteriorActualizada = velneoResponse.PolizaAnteriorActualizada,
                mensajePolizaAnterior = velneoResponse.MensajePolizaAnterior,
                vencimientoValidado = velneoResponse.VencimientoValidado,
                createdAt = DateTime.UtcNow,
                processingTimeMs = processingTimeMs
            };
        }
    }
}