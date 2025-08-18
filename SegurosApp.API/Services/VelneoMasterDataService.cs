using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Text.Json;

namespace SegurosApp.API.Services
{
    public class VelneoMasterDataService : IVelneoMasterDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<VelneoMasterDataService> _logger;

        private string ApiKey => _configuration["VelneoAPI:ApiKey"] ??
            "349THFN38U09428URUHTBG3RNMETJ859JP9";
        private string BaseUrl => _configuration["VelneoAPI:BaseUrl"] ??
            "https://app.uruguaycom.com/apid/Seguros_dat/v1";

        public VelneoMasterDataService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<VelneoMasterDataService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<CombustibleItem>> GetCombustiblesAsync()
        {
            const string cacheKey = "velneo_combustibles";

            if (_cache.TryGetValue(cacheKey, out List<CombustibleItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/combustibles?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCombustibleResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var combustibles = velneoResponse?.combustibles ?? new List<CombustibleItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, combustibles, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Combustibles obtenidos: {Count}", combustibles.Count);
                    return combustibles;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener combustibles: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener combustibles");
            }

            return new List<CombustibleItem>();
        }

        public async Task<List<DepartamentoItem>> GetDepartamentosAsync()
        {
            const string cacheKey = "velneo_departamentos";

            if (_cache.TryGetValue(cacheKey, out List<DepartamentoItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/departamentos?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDepartamentoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var departamentos = velneoResponse?.departamentos ?? new List<DepartamentoItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, departamentos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Departamentos obtenidos: {Count}", departamentos.Count);
                    return departamentos;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener departamentos: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener departamentos");
            }

            return new List<DepartamentoItem>();
        }

        public async Task<List<CorredorItem>> GetCorredoresAsync()
        {
            const string cacheKey = "velneo_corredores";

            if (_cache.TryGetValue(cacheKey, out List<CorredorItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/corredores?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCorredorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var corredores = velneoResponse?.corredores ?? new List<CorredorItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, corredores, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Corredores obtenidos: {Count}", corredores.Count);
                    return corredores;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener corredores: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener corredores");
            }

            return new List<CorredorItem>();
        }

        public async Task<List<CategoriaItem>> GetCategoriasAsync()
        {
            const string cacheKey = "velneo_categorias";

            if (_cache.TryGetValue(cacheKey, out List<CategoriaItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/categorias?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCategoriaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var categorias = velneoResponse?.categorias ?? new List<CategoriaItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, categorias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Categorías obtenidas: {Count}", categorias.Count);
                    return categorias;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener categorías: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener categorías");
            }

            return new List<CategoriaItem>();
        }

        public async Task<List<DestinoItem>> GetDestinosAsync()
        {
            const string cacheKey = "velneo_destinos";

            if (_cache.TryGetValue(cacheKey, out List<DestinoItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/destinos?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDestinoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var destinos = velneoResponse?.destinos ?? new List<DestinoItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, destinos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Destinos obtenidos: {Count}", destinos.Count);
                    return destinos;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener destinos: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener destinos");
            }

            return new List<DestinoItem>();
        }

        public async Task<List<CalidadItem>> GetCalidadesAsync()
        {
            const string cacheKey = "velneo_calidades";

            if (_cache.TryGetValue(cacheKey, out List<CalidadItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/calidades?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCalidadResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var calidades = velneoResponse?.calidades ?? new List<CalidadItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, calidades, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Calidades obtenidas: {Count}", calidades.Count);
                    return calidades;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener calidades: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener calidades");
            }

            return new List<CalidadItem>();
        }

        public async Task<List<TarifaItem>> GetTarifasAsync()
        {
            const string cacheKey = "velneo_tarifas";

            if (_cache.TryGetValue(cacheKey, out List<TarifaItem>? cached) && cached != null)
                return cached;

            try
            {
                var url = $"{BaseUrl}/tarifas?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoTarifaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var tarifas = velneoResponse?.tarifas ?? new List<TarifaItem>();

                    // Cache por 2 horas
                    _cache.Set(cacheKey, tarifas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Tarifas obtenidas: {Count}", tarifas.Count);
                    return tarifas;
                }
                else
                {
                    _logger.LogError("❌ Error al obtener tarifas: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción al obtener tarifas");
            }

            return new List<TarifaItem>();
        }

        public async Task<CompleteMasterDataResponse> GetAllMasterDataAsync()
        {
            const string cacheKey = "velneo_all_master_data";

            if (_cache.TryGetValue(cacheKey, out CompleteMasterDataResponse? cached) && cached != null)
                return cached;

            _logger.LogInformation("🔄 Obteniendo todo el master data de Velneo...");

            // Obtener todo en paralelo
            var departamentosTask = GetDepartamentosAsync();
            var combustiblesTask = GetCombustiblesAsync();
            var corredoresTask = GetCorredoresAsync();
            var categoriasTask = GetCategoriasAsync();
            var destinosTask = GetDestinosAsync();
            var calidadesTask = GetCalidadesAsync();
            var tarifasTask = GetTarifasAsync();

            await Task.WhenAll(departamentosTask, combustiblesTask, corredoresTask,
                             categoriasTask, destinosTask, calidadesTask, tarifasTask);

            var result = new CompleteMasterDataResponse
            {
                Departamentos = await departamentosTask,
                Combustibles = await combustiblesTask,
                Corredores = await corredoresTask,
                Categorias = await categoriasTask,
                Destinos = await destinosTask,
                Calidades = await calidadesTask,
                Tarifas = await tarifasTask,
                EstadosGestion = GetEstadosGestion(),
                Tramites = GetTramites(),
                EstadosPoliza = GetEstadosPoliza(),
                FormasPago = GetFormasPago()
            };

            // Cache completo por 1 hora
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

            _logger.LogInformation("✅ Master data completo obtenido y cacheado");
            return result;
        }

        private List<StaticOption> GetEstadosGestion()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "En emisión" },
                new() { Value = "2", Label = "Pendiente" },
                new() { Value = "3", Label = "En proceso" },
                new() { Value = "4", Label = "Modificaciones" },
                new() { Value = "5", Label = "Terminado" }
            };
        }

        private List<StaticOption> GetTramites()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Nuevo" },
                new() { Value = "2", Label = "Renovación" },
                new() { Value = "3", Label = "Cambio" },
                new() { Value = "4", Label = "Endoso" },
                new() { Value = "5", Label = "No Renueva" },
                new() { Value = "6", Label = "Cancelación" }
            };
        }

        private List<StaticOption> GetEstadosPoliza()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Vigente" },
                new() { Value = "2", Label = "Anulada" },
                new() { Value = "3", Label = "Vencida" },
                new() { Value = "4", Label = "Cancelada" }
            };
        }

        private List<StaticOption> GetFormasPago()
        {
            return new List<StaticOption>
            {
                new() { Value = "1", Label = "Contado" },
                new() { Value = "T", Label = "Tarjeta de Crédito" },
                new() { Value = "E", Label = "Efectivo" },
                new() { Value = "B", Label = "Transferencia Bancaria" },
                new() { Value = "C", Label = "Crédito" }
            };
        }

        public async Task<FieldMappingSuggestion> SuggestMappingAsync(string fieldName, string scannedValue)
        {
            try
            {
                _logger.LogInformation("🧠 Sugiriendo mapeo para {FieldName}: {ScannedValue}",
                    fieldName, scannedValue);

                var masterData = await GetAllMasterDataAsync();

                return fieldName.ToLower() switch
                {
                    "departamento" or "asegurado.departamento" =>
                        FindBestMatch(scannedValue, masterData.Departamentos.Cast<object>().ToList(),
                            item => ((DepartamentoItem)item).dptnom,
                            item => ((DepartamentoItem)item).id.ToString()),

                    "combustible" or "vehiculo.combustible" =>
                        FindBestMatch(scannedValue, masterData.Combustibles.Cast<object>().ToList(),
                            item => ((CombustibleItem)item).name,
                            item => ((CombustibleItem)item).id),

                    "destino" or "vehiculo.destino" =>
                        FindBestMatch(scannedValue, masterData.Destinos.Cast<object>().ToList(),
                            item => ((DestinoItem)item).desnom,
                            item => ((DestinoItem)item).id.ToString()),

                    "categoria" or "vehiculo.categoria" =>
                        FindBestMatch(scannedValue, masterData.Categorias.Cast<object>().ToList(),
                            item => ((CategoriaItem)item).catdsc,
                            item => ((CategoriaItem)item).id.ToString()),

                    "calidad" or "vehiculo.calidad" =>
                        FindBestMatch(scannedValue, masterData.Calidades.Cast<object>().ToList(),
                            item => ((CalidadItem)item).caldsc,
                            item => ((CalidadItem)item).id.ToString()),

                    "corredor" or "corredor.nombre" =>
                        FindBestMatch(scannedValue, masterData.Corredores.Cast<object>().ToList(),
                            item => ((CorredorItem)item).corrnom,
                            item => ((CorredorItem)item).id.ToString()),

                    "forma_pago" or "pago.medio" =>
                        MapFormaPago(scannedValue),

                    _ => new FieldMappingSuggestion
                    {
                        FieldName = fieldName,
                        ScannedValue = scannedValue,
                        SuggestedValue = null,
                        Confidence = 0.0,
                        Source = "NoMatch"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sugiriendo mapeo para {FieldName}", fieldName);
                return new FieldMappingSuggestion
                {
                    FieldName = fieldName,
                    ScannedValue = scannedValue,
                    Confidence = 0.0,
                    Source = "Error"
                };
            }
        }

        private FieldMappingSuggestion FindBestMatch(string scannedValue, List<object> items,
            Func<object, string> getName, Func<object, string> getId)
        {
            var bestMatch = items
                .Select(item => new
                {
                    Item = item,
                    Name = getName(item),
                    Id = getId(item),
                    Similarity = CalculateSimilarity(scannedValue, getName(item))
                })
                .Where(x => x.Similarity >= 0.6) // Mínimo 60% similitud
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                return new FieldMappingSuggestion
                {
                    ScannedValue = scannedValue,
                    SuggestedValue = bestMatch.Id,
                    SuggestedLabel = bestMatch.Name,
                    Confidence = bestMatch.Similarity,
                    Source = "TextSimilarity"
                };
            }

            return new FieldMappingSuggestion
            {
                ScannedValue = scannedValue,
                SuggestedValue = null,
                Confidence = 0.0,
                Source = "NoMatch"
            };
        }

        private FieldMappingSuggestion MapFormaPago(string scannedValue)
        {
            var formaPago = scannedValue.ToUpper();

            if (formaPago.Contains("TARJETA") || formaPago.Contains("CREDITO"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "T",
                    SuggestedLabel = "Tarjeta de Crédito",
                    Confidence = 0.90,
                    Source = "RuleMatch"
                };

            if (formaPago.Contains("CONTADO") || formaPago.Contains("EFECTIVO"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "1",
                    SuggestedLabel = "Contado",
                    Confidence = 0.90,
                    Source = "RuleMatch"
                };

            if (formaPago.Contains("TRANSFERENCIA"))
                return new FieldMappingSuggestion
                {
                    SuggestedValue = "B",
                    SuggestedLabel = "Transferencia Bancaria",
                    Confidence = 0.85,
                    Source = "RuleMatch"
                };

            return new FieldMappingSuggestion
            {
                SuggestedValue = "1",
                SuggestedLabel = "Contado",
                Confidence = 0.50,
                Source = "DefaultValue"
            };
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            text1 = text1.ToLower().Trim();
            text2 = text2.ToLower().Trim();

            if (text1 == text2) return 1.0;
            if (text2.Contains(text1) || text1.Contains(text2)) return 0.85;

            // Algoritmo de Levenshtein simplificado
            var distance = ComputeLevenshteinDistance(text1, text2);
            var maxLength = Math.Max(text1.Length, text2.Length);

            return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; d[i, 0] = i++) { }
            for (var j = 0; j <= m; d[0, j] = j++) { }

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public Task SaveMappingAsync(int userId, string fieldName, string scannedValue, string velneoValue)
        {
            // TODO: Implementar cuando tengamos la tabla UserFieldMappings
            _logger.LogInformation("💾 Guardando mapeo: {FieldName} {ScannedValue} -> {VelneoValue}",
                fieldName, scannedValue, velneoValue);
            return Task.CompletedTask;
        }

        public async Task<CreatePolizaResponse> CreatePolizaAsync(CreatePolizaRequest request)
        {
            try
            {
                _logger.LogInformation("🚀 Creando póliza en Velneo para scan {ScanId}", request.ScanId);

                // TODO: Implementar el POST a Velneo cuando tengamos el mapper completo
                // var velneoContrato = MapToVelneoContrato(request);
                // var response = await PostToVelneo(velneoContrato);

                await Task.Delay(100); // Simular llamada por ahora

                return new CreatePolizaResponse
                {
                    Success = true,
                    Message = "Póliza creada exitosamente",
                    PolizaId = 7651, // Mock
                    PolizaNumber = "12345678",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando póliza para scan {ScanId}", request.ScanId);
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Error creando póliza: {ex.Message}"
                };
            }
        }
    }
}
