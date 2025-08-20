namespace SegurosApp.API.DTOs
{
    public class CreatePolizaVelneoRequest
    {
        // ✅ OVERRIDES BÁSICOS EXISTENTES
        public int ScanId { get; set; }
        public int ClienteId { get; set; }
        public int CompaniaId { get; set; }
        public int SeccionId { get; set; }
        public string? StartDateOverride { get; set; }
        public string? EndDateOverride { get; set; }
        public decimal? PremiumOverride { get; set; }
        public decimal? TotalOverride { get; set; }  // ✅ NUEVO

        // ✅ OVERRIDES DEL VEHÍCULO
        public string? VehicleBrandOverride { get; set; }
        public string? VehicleModelOverride { get; set; }
        public int? VehicleYearOverride { get; set; }
        public string? MotorNumberOverride { get; set; }
        public string? ChassisNumberOverride { get; set; }

        // ✅ OVERRIDES DE PAGO - NUEVOS
        public string? PaymentMethodOverride { get; set; }
        public int? InstallmentCountOverride { get; set; }

        // ✅ OVERRIDES DE MASTER DATA - NUEVOS
        public int? DepartmentIdOverride { get; set; }
        public string? FuelCodeOverride { get; set; }
        public int? DestinationIdOverride { get; set; }
        public int? CategoryIdOverride { get; set; }
        public int? QualityIdOverride { get; set; }
        public int? TariffIdOverride { get; set; }

        // ✅ OVERRIDES DE CLIENTE - NUEVOS
        public string? ClientNameOverride { get; set; }
        public string? ClientAddressOverride { get; set; }

        // ✅ CAMPOS EXISTENTES
        public string? Notes { get; set; }
        public bool ForceCreate { get; set; }
        public List<string> CorrectedFields { get; set; } = new();
        public string? UserComments { get; set; }
    }
}
