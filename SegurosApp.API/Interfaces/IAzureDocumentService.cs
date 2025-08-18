using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo;

namespace SegurosApp.API.Interfaces
{
    public interface IAzureDocumentService
    {
        Task<DocumentScanResponse> ProcessDocumentAsync(IFormFile file, int userId);
        Task<DocumentHistoryDto?> GetScanByIdAsync(int scanId, int userId);
        Task<List<DocumentHistoryDto>> GetScanHistoryAsync(int userId, DocumentSearchFilters filters);
        Task<DocumentMetricsDto> GetDocumentMetricsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<DocumentScanResponse> ReprocessDocumentAsync(int scanId, int userId, bool forceReprocess = false);
        Task UpdateScanWithVelneoInfoAsync(int scanId, string? velneoPolizaNumber, bool velneoCreated);
        Task<List<DocumentHistoryDto>> GetPendingVelneoScansAsync(int userId, int limit = 50);
        Task<VelneoIntegrationMetricsDto> GetVelneoIntegrationMetricsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}