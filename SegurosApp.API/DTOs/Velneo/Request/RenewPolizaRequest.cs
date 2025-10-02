namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class RenewPolizaRequest
    {
        // ✅ CAMPOS EXISTENTES
        public int PolizaAnteriorId { get; set; }
        public string? Observaciones { get; set; }
        public bool ValidarVencimiento { get; set; } = true;
        public int DiasAntesVencimiento { get; set; } = 30;
        public string? CombustibleId { get; set; }
        public string? CategoriaId { get; set; }
        public string? DestinoId { get; set; }
        public string? DepartamentoId { get; set; }
        public string? CalidadId { get; set; }
        public string? TarifaId { get; set; }
        public string? CorredorId { get; set; }
        public string? MonedaId { get; set; }
        public string? NumeroPoliza { get; set; }
        public string? FechaDesde { get; set; }
        public string? FechaHasta { get; set; }
        public decimal? Premio { get; set; }
        public decimal? MontoTotal { get; set; }
        public int? CantidadCuotas { get; set; }
        public decimal? ValorPorCuota { get; set; }
        public string? VehiculoMarca { get; set; }
        public string? VehiculoModelo { get; set; }
        public int? VehiculoAno { get; set; }
        public string? VehiculoPatente { get; set; }
        public string? VehiculoChasis { get; set; }
        public string? VehiculoMotor { get; set; }
        public string? VehiculoPadron { get; set; }
        public List<string> CamposCorregidos { get; set; } = new();
        public string? ComentariosUsuario { get; set; }
        public bool ForzarRenovacion { get; set; } = false;
    }
}