namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class CreatePolizaRequest
    {
        public int ScanId { get; set; } 
        public int CompaniaId { get; set; }
        public int ClienteId { get; set; }
        public int? TomadorId { get; set; }
        public int CorredorId { get; set; }
        public int CategoriaId { get; set; }
        public int DestinoId { get; set; }
        public int CalidadId { get; set; }
        public int DepartamentoId { get; set; }
        public int TarifaId { get; set; }
        public string CombustibleId { get; set; } = string.Empty;
        public string EstadoGestion { get; set; } = "1";
        public string Tramite { get; set; } = "1"; 
        public string EstadoPoliza { get; set; } = "1"; 
        public string FormaPago { get; set; } = "1"; 
        public Dictionary<string, object> CamposAdicionales { get; set; } = new();
    }
}
