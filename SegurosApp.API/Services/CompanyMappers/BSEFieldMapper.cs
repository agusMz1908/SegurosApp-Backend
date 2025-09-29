using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class BSEFieldMapper : BaseFieldMapper
    {
        public BSEFieldMapper(ILogger<BSEFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "BSE";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            _logger.LogDebug("Normalizando campos BSE (formato estándar)");

            var normalized = new Dictionary<string, object>(extractedData);
            MapStandardBSEFields(normalized);

            return await Task.FromResult(normalized);
        }

        private void MapStandardBSEFields(Dictionary<string, object> data)
        {
            _logger.LogDebug("Campos BSE mapeados (formato estándar)");
        }
    }
}