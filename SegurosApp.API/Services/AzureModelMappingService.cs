using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services
{
    public class AzureModelMappingService : IAzureModelMappingService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly ILogger<AzureModelMappingService> _logger;
        private readonly IMemoryCache _cache;

        private readonly Dictionary<string, string> _companiaToModelMap = new()
        {
            { "BSE", "poliza_vehiculos_bse" },
            { "SURA", "poliza_vechiulos_sura" },
            { "MAPFRE", "poliza_vehiculos_mapfre" }
        };

        public AzureModelMappingService(
            IVelneoMasterDataService masterDataService,
            ILogger<AzureModelMappingService> logger,
            IMemoryCache cache)
        {
            _masterDataService = masterDataService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<AzureModelInfo> GetModelByCompaniaIdAsync(int companiaId)
        {
            const string cacheKey = "azure_model_mapping_cache";

            try
            {
                if (_cache.TryGetValue($"{cacheKey}_{companiaId}", out AzureModelInfo? cachedModel) && cachedModel != null)
                {
                    _logger.LogDebug("Modelo obtenido del cache para compañía {CompaniaId}: {ModelId}",
                        companiaId, cachedModel.ModelId);
                    return cachedModel;
                }

                _logger.LogInformation("Buscando modelo Azure para compañía ID: {CompaniaId}", companiaId);

                var masterData = await _masterDataService.GetAllMasterDataAsync();
                var compania = masterData.Companias.FirstOrDefault(c => c.id == companiaId);

                if (compania == null)
                {
                    var defaultModel = GetDefaultModelInfo();
                    _logger.LogWarning("Compañía {CompaniaId} no encontrada. Usando modelo por defecto: {ModelId}",
                        companiaId, defaultModel.ModelId);
                    return defaultModel;
                }

                var companiaDisplayName = compania.DisplayName.ToUpperInvariant();

                string? modelId = null;
                string? companiaAlias = null;

                if (companiaDisplayName.Contains("BANCO") && companiaDisplayName.Contains("SEGUROS"))
                {
                    modelId = "poliza_vehiculos_bse";
                    companiaAlias = "BSE";
                }
                else if (companiaDisplayName.Contains("SURA"))
                {
                    modelId = "poliza_vechiulos_sura";
                    companiaAlias = "SURA";
                }
                else if (companiaDisplayName.Contains("MAPFRE"))
                {
                    modelId = "poliza_vehiculos_mapfre";
                    companiaAlias = "MAPFRE";
                }

                var result = modelId != null
                    ? new AzureModelInfo
                    {
                        ModelId = modelId,
                        ModelName = GetModelDisplayName(modelId),
                        CompaniaAlias = companiaAlias!,
                        CompaniaId = companiaId,
                        Description = $"Modelo entrenado para pólizas de {compania.DisplayName}",
                        IsActive = true
                    }
                    : GetDefaultModelInfo();

                if (modelId == null)
                {
                    _logger.LogWarning("No se encontró modelo específico para compañía {CompaniaName} (ID: {CompaniaId}). Usando modelo por defecto",
                        compania.DisplayName, companiaId);
                }
                else
                {
                    _logger.LogInformation("Modelo encontrado para {CompaniaName} (ID: {CompaniaId}): {ModelId}",
                        compania.DisplayName, companiaId, result.ModelId);
                }

                _cache.Set($"{cacheKey}_{companiaId}", result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo modelo para compañía {CompaniaId}. Usando modelo por defecto", companiaId);
                return GetDefaultModelInfo();
            }
        }

        public async Task<List<AzureModelInfo>> GetAllAvailableModelsAsync()
        {
            try
            {
                var masterData = await _masterDataService.GetAllMasterDataAsync();
                var modelInfoList = new List<AzureModelInfo>();

                foreach (var kvp in _companiaToModelMap)
                {
                    var compania = masterData.Companias.FirstOrDefault(c =>
                    {
                        var displayName = c.DisplayName.ToUpperInvariant();
                        var alias = kvp.Key.ToUpperInvariant();

                        return alias switch
                        {
                            "BSE" => displayName.Contains("BANCO") && displayName.Contains("SEGUROS"),
                            "SURA" => displayName.Contains("SURA"),
                            "MAPFRE" => displayName.Contains("MAPFRE"),
                            _ => displayName.Contains(alias)
                        };
                    });

                    modelInfoList.Add(new AzureModelInfo
                    {
                        ModelId = kvp.Value,
                        ModelName = GetModelDisplayName(kvp.Value),
                        CompaniaAlias = kvp.Key,
                        CompaniaId = compania?.id ?? 0,
                        Description = $"Modelo entrenado para pólizas de {kvp.Key}",
                        IsActive = true
                    });
                }

                _logger.LogInformation("Obtenidos {Count} modelos Azure disponibles", modelInfoList.Count);
                return modelInfoList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo modelos disponibles");
                return new List<AzureModelInfo>();
            }
        }

        public async Task<bool> HasModelForCompaniaAsync(int companiaId)
        {
            try
            {
                var modelInfo = await GetModelByCompaniaIdAsync(companiaId);
                return modelInfo.ModelId != "poliza_vehiculos_bse" || companiaId == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando disponibilidad de modelo para compañía {CompaniaId}", companiaId);
                return false;
            }
        }

        private static AzureModelInfo GetDefaultModelInfo()
        {
            return new AzureModelInfo
            {
                ModelId = "poliza_vehiculos_bse",
                ModelName = "Pólizas BSE (Por defecto)",
                CompaniaAlias = "BSE",
                CompaniaId = 1,
                Description = "Modelo por defecto para pólizas de vehículos",
                IsActive = true
            };
        }

        private static string GetModelDisplayName(string modelId)
        {
            return modelId switch
            {
                "poliza_vehiculos_bse" => "Pólizas BSE",
                "poliza_vechiulos_sura" => "Pólizas SURA",
                "poliza_vehiculos_mapfre" => "Pólizas MAPFRE",
                "poliza_vehiculo_porto" => "Pólizas PORTO",
                _ => $"Modelo {modelId}"
            };
        }
    }
}