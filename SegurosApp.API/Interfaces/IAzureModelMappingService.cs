using SegurosApp.API.DTOs;

namespace SegurosApp.API.Interfaces
{
    public interface IAzureModelMappingService
    {
        Task<AzureModelInfo> GetModelByCompaniaIdAsync(int companiaId);
        Task<List<AzureModelInfo>> GetAllAvailableModelsAsync();
        Task<bool> HasModelForCompaniaAsync(int companiaId);
    }
}