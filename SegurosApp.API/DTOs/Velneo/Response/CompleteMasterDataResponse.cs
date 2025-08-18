using SegurosApp.API.DTOs.Velneo.Item;

namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class CompleteMasterDataResponse
    {
        public List<DepartamentoItem> Departamentos { get; set; } = new();
        public List<CombustibleItem> Combustibles { get; set; } = new();
        public List<CorredorItem> Corredores { get; set; } = new();
        public List<CategoriaItem> Categorias { get; set; } = new();
        public List<DestinoItem> Destinos { get; set; } = new();
        public List<CalidadItem> Calidades { get; set; } = new();
        public List<TarifaItem> Tarifas { get; set; } = new();
        public List<StaticOption> EstadosGestion { get; set; } = new();
        public List<StaticOption> Tramites { get; set; } = new();
        public List<StaticOption> EstadosPoliza { get; set; } = new();
        public List<StaticOption> FormasPago { get; set; } = new();
    }

    public class StaticOption
    {
        public string Value { get; set; } = string.Empty; 
        public string Label { get; set; } = string.Empty; 
        public string? Description { get; set; }
    }

    public class FieldMappingSuggestion
    {
        public string FieldName { get; set; } = string.Empty;
        public string ScannedValue { get; set; } = string.Empty;
        public string? SuggestedValue { get; set; }
        public string? SuggestedLabel { get; set; } 
        public double Confidence { get; set; }
        public string Source { get; set; } = string.Empty; 
        public List<object> Alternatives { get; set; } = new();
    }
}
