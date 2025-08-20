using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;

namespace SegurosApp.API.Interfaces
{
    public interface IVelneoMasterDataService
    {
        Task<List<DepartamentoItem>> GetDepartamentosAsync();
        Task<List<CombustibleItem>> GetCombustiblesAsync();
        Task<List<CorredorItem>> GetCorredoresAsync();
        Task<List<CategoriaItem>> GetCategoriasAsync();
        Task<List<DestinoItem>> GetDestinosAsync();
        Task<List<CalidadItem>> GetCalidadesAsync();
        Task<List<TarifaItem>> GetTarifasAsync();
        Task<List<MonedaItem>> GetMonedasAsync();
        Task<CompleteMasterDataResponse> GetAllMasterDataAsync();
        Task<FieldMappingSuggestion> SuggestMappingAsync(string fieldName, string scannedValue);
        Task SaveMappingAsync(int userId, string fieldName, string scannedValue, string velneoValue);
        Task<CreatePolizaResponse> CreatePolizaAsync(VelneoPolizaRequest request);
        Task<List<ClienteItem>> SearchClientesAsync(string query, int limit = 20);
        Task<ClienteItem?> GetClienteDetalleAsync(int clienteId);
        Task<List<CompaniaItem>> GetCompaniasAsync();
        Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null);
        Task<List<ClienteItem>> AdvancedSearchClientesAsync(ClienteSearchFilters filters);
    }
}