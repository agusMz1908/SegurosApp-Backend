using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.DTOs
{
    public class PolizaSearchFilters
    {
        [StringLength(50, ErrorMessage = "El número de póliza no puede exceder 50 caracteres")]
        public string? NumeroPoliza { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a 0")]
        public int? ClienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El ID de la compañía debe ser mayor a 0")]
        public int? CompaniaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El ID de la sección debe ser mayor a 0")]
        public int? SeccionId { get; set; }

        [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres")]
        public string? Estado { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaDesde { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; }

        public bool SoloActivos { get; set; } = true;

        [Range(1, 1000, ErrorMessage = "El límite debe estar entre 1 y 1000")]
        public int Limit { get; set; } = 20;

        public void TrimAndCleanFilters()
        {
            NumeroPoliza = string.IsNullOrWhiteSpace(NumeroPoliza) ? null : NumeroPoliza.Trim();
            Estado = string.IsNullOrWhiteSpace(Estado) ? null : Estado.Trim();
        }

        public bool HasAnyFilter()
        {
            return !string.IsNullOrEmpty(NumeroPoliza) ||
                   ClienteId.HasValue ||
                   CompaniaId.HasValue ||
                   SeccionId.HasValue ||
                   !string.IsNullOrEmpty(Estado) ||
                   FechaDesde.HasValue ||
                   FechaHasta.HasValue;
        }

        public string GetCacheKey()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(NumeroPoliza)) parts.Add($"pol_{NumeroPoliza}");
            if (ClienteId.HasValue) parts.Add($"cli_{ClienteId}");
            if (CompaniaId.HasValue) parts.Add($"com_{CompaniaId}");
            if (SeccionId.HasValue) parts.Add($"sec_{SeccionId}");
            if (!string.IsNullOrEmpty(Estado)) parts.Add($"est_{Estado}");
            if (FechaDesde.HasValue) parts.Add($"desde_{FechaDesde.Value:yyyyMMdd}");
            if (FechaHasta.HasValue) parts.Add($"hasta_{FechaHasta.Value:yyyyMMdd}");
            if (SoloActivos) parts.Add("activos");

            return parts.Count > 0 ? string.Join("_", parts) : "all";
        }

        public int GetActiveFiltersCount()
        {
            int count = 0;
            if (!string.IsNullOrEmpty(NumeroPoliza)) count++;
            if (ClienteId.HasValue) count++;
            if (CompaniaId.HasValue) count++;
            if (SeccionId.HasValue) count++;
            if (!string.IsNullOrEmpty(Estado)) count++;
            if (FechaDesde.HasValue) count++;
            if (FechaHasta.HasValue) count++;
            return count;
        }

        public override string ToString()
        {
            var filters = new List<string>();

            if (!string.IsNullOrEmpty(NumeroPoliza)) filters.Add($"Número: {NumeroPoliza}");
            if (ClienteId.HasValue) filters.Add($"Cliente: {ClienteId}");
            if (CompaniaId.HasValue) filters.Add($"Compañía: {CompaniaId}");
            if (SeccionId.HasValue) filters.Add($"Sección: {SeccionId}");
            if (!string.IsNullOrEmpty(Estado)) filters.Add($"Estado: {Estado}");
            if (FechaDesde.HasValue) filters.Add($"Desde: {FechaDesde.Value:yyyy-MM-dd}");
            if (FechaHasta.HasValue) filters.Add($"Hasta: {FechaHasta.Value:yyyy-MM-dd}");
            if (SoloActivos) filters.Add("Solo activos");

            return filters.Count > 0 ? string.Join(", ", filters) : "Sin filtros";
        }
    }
}