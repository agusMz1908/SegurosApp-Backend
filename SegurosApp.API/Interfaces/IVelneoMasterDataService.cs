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
        Task<VelneoPaginatedResponse<ContratoItem>> GetPolizasPaginatedAsync(int page = 1, int pageSize = 20, PolizaSearchFilters? filters = null);
        Task<List<ContratoItem>> SearchPolizasQuickAsync(string numeroPoliza, int limit = 10);
        Task<ContratoItem?> GetPolizaDetalleAsync(int polizaId);

        Task<ClienteItem?> GetClienteDetalleAsync(int clienteId);
        Task<List<CompaniaItem>> GetCompaniasAsync();
        Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null);
        Task<VelneoPaginatedResponse<ClienteItem>> GetClientesPaginatedAsync(int page = 1, int pageSize = 20, ClienteSearchFilters? filters = null);
        Task<List<ClienteItem>> SearchClientesQuickAsync(string query, int limit = 10);

        Task<UpdatePolizaResponse> UpdatePolizaEstadosAsync(int polizaId);
        Task<ModifyPolizaResponse> ModifyPolizaAsync(VelneoPolizaRequest request, int polizaAnteriorId, string tipoCambio, string? observaciones = null);
    }
}