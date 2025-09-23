namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class ModifyPolizaRequest
    {
        // ✅ CAMPOS EXISTENTES
        public int PolizaAnteriorId { get; set; }
        public string TipoCambio { get; set; } = "";
        public string? Observaciones { get; set; }

        // ✅ AGREGAR ESTAS PROPIEDADES (siguiendo el patrón de RenewPolizaRequest)
        public string? CombustibleId { get; set; }
        public int? CategoriaId { get; set; }
        public int? DestinoId { get; set; }
        public int? DepartamentoId { get; set; }
        public int? CalidadId { get; set; }
        public int? TarifaId { get; set; }
        public int? CorredorId { get; set; }
        public int? MonedaId { get; set; }
        public string? NumeroPoliza { get; set; }
        public string? FechaDesde { get; set; }
        public string? FechaHasta { get; set; }
        public decimal? Premio { get; set; }
        public decimal? MontoTotal { get; set; }
        public int? CantidadCuotas { get; set; }
        public string? VehiculoMarca { get; set; }
        public string? VehiculoModelo { get; set; }
        public int? VehiculoAno { get; set; }
        public string? VehiculoPatente { get; set; }
        public string? VehiculoChasis { get; set; }
        public string? VehiculoMotor { get; set; }
        public string? ComentariosUsuario { get; set; }

        // ✅ NUEVAS PROPIEDADES ESPECÍFICAS PARA CAMBIOS
        public string? PolizaAnteriorNumero { get; set; }  // Para las observaciones
        public List<string> CamposCorregidos { get; set; } = new();
        public bool ForzarCambio { get; set; } = false;
    }
}