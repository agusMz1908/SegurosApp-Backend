namespace SegurosApp.API.DTOs.Velneo.Validation
{
    public class ExistingPolizaInfo
    {
        public int Id { get; set; }
        public string NumeroPoliza { get; set; } = "";
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string Estado { get; set; } = "";
        public string EstadoDescripcion { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public decimal? MontoTotal { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? UltimaModificacion { get; set; }
    }
}
