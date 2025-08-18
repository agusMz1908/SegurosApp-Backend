namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class PolizaMappingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PolizaDataMapped MappedData { get; set; } = new();
        public List<FieldMappingIssue> MappingIssues { get; set; } = new();
        public List<FieldSuggestion> Suggestions { get; set; } = new();
        public MappingMetrics Metrics { get; set; } = new();
        public FormData FormData { get; set; } = new();
    }

    public class PolizaDataMapped
    {
        public string NumeroPoliza { get; set; } = "";
        public string Endoso { get; set; } = "0";
        public string FechaDesde { get; set; } = "";
        public string FechaHasta { get; set; } = "";
        public decimal Premio { get; set; } = 0;
        public decimal MontoTotal { get; set; } = 0;
        public string Moneda { get; set; } = "UYU";
        public string AseguradoNombre { get; set; } = "";
        public string AseguradoDocumento { get; set; } = "";
        public string AseguradoDepartamento { get; set; } = "";
        public string AseguradoDireccion { get; set; } = "";
        public string VehiculoMarca { get; set; } = "";
        public string VehiculoModelo { get; set; } = "";
        public int VehiculoAño { get; set; } = 0;
        public string VehiculoMotor { get; set; } = "";
        public string VehiculoChasis { get; set; } = "";
        public string VehiculoCombustible { get; set; } = "";
        public string VehiculoDestino { get; set; } = "";
        public string VehiculoCategoria { get; set; } = "";
        public string CorredorNombre { get; set; } = "";
        public string CorredorNumero { get; set; } = "";
        public string MedioPago { get; set; } = "";
        public int CantidadCuotas { get; set; } = 1;
        public string TipoMovimiento { get; set; } = "";
    }

    public class FieldMappingIssue
    {
        public string FieldName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ScannedValue { get; set; } = "";
        public string IssueType { get; set; } = ""; 
        public string Description { get; set; } = "";
        public string Severity { get; set; } = ""; 
        public bool IsRequired { get; set; } = false;
    }
    public class FieldSuggestion
    {
        public string FieldName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ScannedValue { get; set; } = "";
        public string SuggestedValue { get; set; } = "";
        public string SuggestedLabel { get; set; } = "";
        public double Confidence { get; set; } = 0.0;
        public string Source { get; set; } = ""; 
        public List<AlternativeSuggestion> Alternatives { get; set; } = new();
    }

    public class AlternativeSuggestion
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public double Confidence { get; set; } = 0.0;
    }

    public class MappingMetrics
    {
        public int TotalFieldsScanned { get; set; } = 0;
        public int FieldsMappedSuccessfully { get; set; } = 0;
        public int FieldsWithIssues { get; set; } = 0;
        public int FieldsRequireAttention { get; set; } = 0;
        public decimal OverallConfidence { get; set; } = 0.0M;
        public string MappingQuality { get; set; } = ""; 
        public List<string> MissingCriticalFields { get; set; } = new();
    }

    public class FormData
    {
        public List<ClientOption> AvailableClients { get; set; } = new();
        public List<BrokerOption> AvailableBrokers { get; set; } = new();
        public List<CompanyOption> AvailableCompanies { get; set; } = new();
        public List<DepartmentOption> AvailableDepartments { get; set; } = new();
        public List<FuelOption> AvailableFuels { get; set; } = new();
        public List<DestinationOption> AvailableDestinations { get; set; } = new();
        public List<CategoryOption> AvailableCategories { get; set; } = new();
        public List<QualityOption> AvailableQualities { get; set; } = new();
        public List<TariffOption> AvailableTariffs { get; set; } = new();
        public List<PaymentMethodOption> AvailablePaymentMethods { get; set; } = new();
    }

    public class ClientOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Document { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class BrokerOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class CompanyOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public class DepartmentOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class FuelOption
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class DestinationOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class CategoryOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }

    public class QualityOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public class TariffOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public class PaymentMethodOption
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public double MatchConfidence { get; set; } = 0.0;
    }
}
