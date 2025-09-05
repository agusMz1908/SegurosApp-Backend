using System.Text.Json.Serialization;

namespace SegurosApp.API.DTOs.Velneo.Item
{
    public class ContratoItem
    {
        public int id { get; set; }
        public int comcod { get; set; }
        public int seccod { get; set; }
        public int clinro { get; set; }
        public string condom { get; set; } = string.Empty;
        public string conpol { get; set; } = string.Empty;
        public string? confchdes { get; set; }
        public string? confchhas { get; set; }
        public decimal conpremio { get; set; }
        public string? conmoneda { get; set; }
        public string? conestado { get; set; }
        public bool activo { get; set; } = true;
        public DateTime? fecha_desde { get; set; }
        public DateTime? fecha_hasta { get; set; }
        public DateTime? fecha_emision { get; set; }
        public DateTime? ingresado { get; set; }
        public DateTime? last_update { get; set; }
        public string? cliente_nombre { get; set; }
        public string? cliente_documento { get; set; }
        public string? compania_nombre { get; set; }
        public string? seccion_nombre { get; set; }
        public string? vehiculo_marca { get; set; }
        public string? vehiculo_modelo { get; set; }
        public string? vehiculo_anio { get; set; }
        public string? vehiculo_matricula { get; set; }
        public string? vehiculo_chasis { get; set; }
        public string? vehiculo_motor { get; set; }
        public string? observaciones { get; set; }
        public string? tipo_cobertura { get; set; }
        public decimal? suma_asegurada { get; set; }
        public decimal? deducible { get; set; }

        [JsonIgnore]
        public string DisplayName => !string.IsNullOrEmpty(conpol)
            ? $"Póliza {conpol}"
            : $"Contrato {id}";

        [JsonIgnore]
        public string EstadoDisplay => conestado switch
        {
            "V" => "Vigente",
            "A" => "Anulada",
            "S" => "Suspendida",
            "C" => "Cancelada",
            "E" => "Emitida",
            _ => conestado ?? "Sin estado"
        };

        [JsonIgnore]
        public string MonedaDisplay => conmoneda switch
        {
            "UYU" => "Pesos Uruguayos",
            "USD" => "Dólares",
            "UI" => "Unidades Indexadas",
            _ => conmoneda ?? "No especificada"
        };

        [JsonIgnore]
        public bool EsVigente => conestado == "V" && activo;

        [JsonIgnore]
        public string PeriodoVigencia
        {
            get
            {
                if (fecha_desde.HasValue && fecha_hasta.HasValue)
                    return $"{fecha_desde.Value:dd/MM/yyyy} - {fecha_hasta.Value:dd/MM/yyyy}";
                return "No especificado";
            }
        }

        [JsonIgnore]
        public string PremioFormateado
        {
            get
            {
                var simbolo = conmoneda switch
                {
                    "USD" => "US$ ",
                    "UI" => "UI ",
                    _ => "$ "
                };
                return $"{simbolo}{conpremio:N2}";
            }
        }
    }
}