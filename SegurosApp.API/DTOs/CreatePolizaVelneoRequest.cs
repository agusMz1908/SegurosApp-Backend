namespace SegurosApp.API.DTOs
{
    public class CreatePolizaVelneoRequest
    {
        public int ScanId { get; set; }
        public int ClienteId { get; set; }
        public int CompaniaId { get; set; }
        public int SeccionId { get; set; }
        public string? StartDateOverride { get; set; }
        public string? EndDateOverride { get; set; }
        public decimal? PremiumOverride { get; set; }
        public decimal? TotalOverride { get; set; }  
        public string? VehicleBrandOverride { get; set; }
        public string? VehicleModelOverride { get; set; }
        public int? VehicleYearOverride { get; set; }
        public string? MotorNumberOverride { get; set; }
        public string? ChassisNumberOverride { get; set; }
        public string? VehiclePadronOverride { get; set; }
        public string? PaymentMethodOverride { get; set; }
        public int? InstallmentCountOverride { get; set; }
        public int? DepartmentIdOverride { get; set; }
        public string? FuelCodeOverride { get; set; }
        public int? DestinationIdOverride { get; set; }
        public int? CategoryIdOverride { get; set; }
        public int? QualityIdOverride { get; set; }
        public int? TariffIdOverride { get; set; }
        public int? BrokerIdOverride { get; set; }         
        public int? CurrencyIdOverride { get; set; }       
        public int? PaymentCurrencyIdOverride { get; set; }
        public string? PolicyNumber { get; set; }
        public string? ClientNameOverride { get; set; }
        public string? ClientAddressOverride { get; set; }
        public string? Notes { get; set; }
        public bool ForceCreate { get; set; }
        public List<string> CorrectedFields { get; set; } = new();
        public string? UserComments { get; set; }
    }
}
