using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.Converters;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Text;
using System.Text.Json;

namespace SegurosApp.API.Services
{
    public class MultiTenantVelneoService : IVelneoMasterDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantService _tenantService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MultiTenantVelneoService> _logger;

        // URL default desde configuración (fallback)
        private string DefaultBaseUrl => _configuration["VelneoAPI:BaseUrl"];

        public MultiTenantVelneoService(
            IHttpClientFactory httpClientFactory,
            ITenantService tenantService,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<MultiTenantVelneoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantService = tenantService;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        // ===============================================
        // Métodos auxiliares para Multi-Tenant
        // ===============================================

        private async Task<(string baseUrl, string apiKey)> GetTenantConfigAsync()
        {
            var userId = _tenantService.GetCurrentTenantUserId();
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o UserId no encontrado");
            }

            var apiKey = await _tenantService.GetTenantApiKeyAsync(userId.Value);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new UnauthorizedAccessException($"API Key de Velneo no configurada para el usuario {userId}");
            }

            var baseUrl = await _tenantService.GetTenantBaseUrlAsync(userId.Value) ?? DefaultBaseUrl;
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("URL base de Velneo no configurada");
            }

            _logger.LogDebug("🏢 Usando configuración de tenant - UserId: {UserId}, BaseUrl: {BaseUrl}", userId, baseUrl);

            return (baseUrl, apiKey);
        }

        private async Task<HttpClient> CreateTenantHttpClientAsync()
        {
            var (baseUrl, apiKey) = await GetTenantConfigAsync();
            var client = _httpClientFactory.CreateClient("MultiTenantVelneo");

            // DEBUG: Verificar timeout actual
            _logger.LogWarning("DEBUG: HttpClient timeout: {Timeout} segundos", client.Timeout.TotalSeconds);

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("ApiKey", apiKey);
            // NO agregues User-Agent aquí (ya está en Program.cs)

            return client;
        }

        private string GetTenantCacheKey(string baseKey)
        {
            var userId = _tenantService.GetCurrentTenantUserId();
            return $"{baseKey}_tenant_{userId}";
        }

        // ===============================================
        // Implementación de IVelneoMasterDataService
        // ===============================================

        public async Task<List<DepartamentoItem>> GetDepartamentosAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_departamentos");

            if (_cache.TryGetValue(cacheKey, out List<DepartamentoItem>? cached) && cached != null)
            {
                _logger.LogDebug("💾 Departamentos desde cache para tenant");
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/departamentos?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDepartamentoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var departamentos = velneoResponse?.departamentos ?? new List<DepartamentoItem>();
                    _cache.Set(cacheKey, departamentos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Departamentos obtenidos para tenant: {Count}", departamentos.Count);
                    return departamentos;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo departamentos para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo departamentos para tenant");
            }

            return new List<DepartamentoItem>();
        }

        public async Task<List<CombustibleItem>> GetCombustiblesAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_combustibles");

            if (_cache.TryGetValue(cacheKey, out List<CombustibleItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/combustibles?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCombustibleResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var combustibles = velneoResponse?.combustibles ?? new List<CombustibleItem>();
                    _cache.Set(cacheKey, combustibles, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Combustibles obtenidos para tenant: {Count}", combustibles.Count);
                    return combustibles;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo combustibles para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo combustibles para tenant");
            }

            return new List<CombustibleItem>();
        }

        public async Task<List<CorredorItem>> GetCorredoresAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_corredores");

            if (_cache.TryGetValue(cacheKey, out List<CorredorItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/corredores?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCorredorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var corredores = velneoResponse?.corredores ?? new List<CorredorItem>();
                    _cache.Set(cacheKey, corredores, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Corredores obtenidos para tenant: {Count}", corredores.Count);
                    return corredores;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo corredores para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo corredores para tenant");
            }

            return new List<CorredorItem>();
        }

        public async Task<List<CategoriaItem>> GetCategoriasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_categorias");

            if (_cache.TryGetValue(cacheKey, out List<CategoriaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/categorias?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCategoriaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var categorias = velneoResponse?.categorias ?? new List<CategoriaItem>();
                    _cache.Set(cacheKey, categorias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Categorías obtenidas para tenant: {Count}", categorias.Count);
                    return categorias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo categorías para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo categorías para tenant");
            }

            return new List<CategoriaItem>();
        }

        public async Task<List<DestinoItem>> GetDestinosAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_destinos");

            if (_cache.TryGetValue(cacheKey, out List<DestinoItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/destinos?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoDestinoResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var destinos = velneoResponse?.destinos ?? new List<DestinoItem>();
                    _cache.Set(cacheKey, destinos, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Destinos obtenidos para tenant: {Count}", destinos.Count);
                    return destinos;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo destinos para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo destinos para tenant");
            }

            return new List<DestinoItem>();
        }

        public async Task<List<CalidadItem>> GetCalidadesAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_calidades");

            if (_cache.TryGetValue(cacheKey, out List<CalidadItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/calidades?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCalidadResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var calidades = velneoResponse?.calidades ?? new List<CalidadItem>();
                    _cache.Set(cacheKey, calidades, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Calidades obtenidas para tenant: {Count}", calidades.Count);
                    return calidades;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo calidades para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo calidades para tenant");
            }

            return new List<CalidadItem>();
        }

        public async Task<List<TarifaItem>> GetTarifasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_tarifas");

            if (_cache.TryGetValue(cacheKey, out List<TarifaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/tarifas?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoTarifaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var tarifas = velneoResponse?.tarifas ?? new List<TarifaItem>();
                    _cache.Set(cacheKey, tarifas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Tarifas obtenidas para tenant: {Count}", tarifas.Count);
                    return tarifas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo tarifas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tarifas para tenant");
            }

            return new List<TarifaItem>();
        }

        public async Task<List<MonedaItem>> GetMonedasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_monedas");

            if (_cache.TryGetValue(cacheKey, out List<MonedaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/monedas?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoMonedaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var monedas = velneoResponse?.monedas ?? new List<MonedaItem>();
                    _cache.Set(cacheKey, monedas, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Monedas obtenidas para tenant: {Count}", monedas.Count);
                    return monedas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo monedas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo monedas para tenant");
            }

            return new List<MonedaItem>();
        }

        public async Task<CompleteMasterDataResponse> GetAllMasterDataAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Obteniendo master data completo para tenant");

                // Ejecutar todas las consultas en paralelo con tipos específicos
                var departamentosTask = GetDepartamentosAsync();
                var combustiblesTask = GetCombustiblesAsync();
                var corredoresTask = GetCorredoresAsync();
                var categoriasTask = GetCategoriasAsync();
                var destinosTask = GetDestinosAsync();
                var calidadesTask = GetCalidadesAsync();
                var tarifasTask = GetTarifasAsync();
                var monedasTask = GetMonedasAsync();
                var companiasTask = GetCompaniasAsync();
                var seccionesTask = GetSeccionesAsync();

                // Esperar a que todas terminen
                await Task.WhenAll(
                    departamentosTask,
                    combustiblesTask,
                    corredoresTask,
                    categoriasTask,
                    destinosTask,
                    calidadesTask,
                    tarifasTask,
                    monedasTask,
                    companiasTask,
                    seccionesTask
                );

                // Obtener los resultados
                var response = new CompleteMasterDataResponse
                {
                    Departamentos = await departamentosTask,
                    Combustibles = await combustiblesTask,
                    Corredores = await corredoresTask,
                    Categorias = await categoriasTask,
                    Destinos = await destinosTask,
                    Calidades = await calidadesTask,
                    Tarifas = await tarifasTask,
                    Monedas = await monedasTask,
                    Companias = await companiasTask,
                    Secciones = await seccionesTask
                };

                _logger.LogInformation("✅ Master data completo obtenido para tenant: {Departamentos} dept, {Combustibles} comb, {Corredores} corr",
                    response.Departamentos.Count, response.Combustibles.Count, response.Corredores.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo para tenant");
                throw;
            }
        }

        public async Task<List<CompaniaItem>> GetCompaniasAsync()
        {
            var cacheKey = GetTenantCacheKey("velneo_companias");

            if (_cache.TryGetValue(cacheKey, out List<CompaniaItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var response = await client.GetAsync($"v1/companias?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoCompaniaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var companias = velneoResponse?.companias ?? new List<CompaniaItem>();
                    _cache.Set(cacheKey, companias, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Compañías obtenidas para tenant: {Count}", companias.Count);
                    return companias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo compañías para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo compañías para tenant");
            }

            return new List<CompaniaItem>();
        }

        public async Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null)
        {
            var cacheKey = GetTenantCacheKey($"velneo_secciones_{companiaId}");

            if (_cache.TryGetValue(cacheKey, out List<SeccionItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var url = companiaId.HasValue
                    ? $"v1/secciones?compania_id={companiaId}&api_key={apiKey}"
                    : $"v1/secciones?api_key={apiKey}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoSeccionResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var secciones = velneoResponse?.secciones ?? new List<SeccionItem>();
                    _cache.Set(cacheKey, secciones, TimeSpan.FromHours(2));

                    _logger.LogInformation("✅ Secciones obtenidas para tenant (CompañíaId: {CompaniaId}): {Count}",
                        companiaId, secciones.Count);
                    return secciones;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo secciones para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo secciones para tenant");
            }

            return new List<SeccionItem>();
        }

        public async Task<FieldMappingSuggestion> SuggestMappingAsync(string fieldName, string scannedValue)
        {
            // TODO: Implementar lógica de sugerencia basada en datos del tenant
            // Por ahora retorna sugerencia básica
            return new FieldMappingSuggestion
            {
                FieldName = fieldName,
                ScannedValue = scannedValue,
                SuggestedValue = scannedValue,
                Confidence = (double)0.5m,
            };
        }

        public async Task SaveMappingAsync(int userId, string fieldName, string scannedValue, string velneoValue)
        {
            // TODO: Implementar guardado de mappings personalizados por tenant
            _logger.LogInformation("💾 Guardando mapping para tenant UserId {UserId}: {FieldName} = {VelneoValue}",
                userId, fieldName, velneoValue);
        }

        public async Task<VelneoPaginatedResponse<ContratoItem>> GetPolizasPaginatedAsync(
            int page = 1,
            int pageSize = 20,
            PolizaSearchFilters? filters = null)
        {
            var cacheKey = GetTenantCacheKey($"polizas_page_{page}_{pageSize}_{filters?.GetCacheKey() ?? "all"}");

            if (_cache.TryGetValue(cacheKey, out VelneoPaginatedResponse<ContratoItem>? cached) && cached != null)
            {
                _logger.LogDebug("💾 Pólizas paginadas desde cache para tenant - Página: {Page}", page);
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var queryParams = new List<string>
                {
                    $"api_key={apiKey}",
                    $"page[number]={page}",
                    $"page[size]={pageSize}"
                };

                // Aplicar filtros si existen
                if (filters != null)
                {
                    if (!string.IsNullOrEmpty(filters.NumeroPoliza))
                        queryParams.Add($"filter[poliza]={Uri.EscapeDataString(filters.NumeroPoliza)}");

                    if (filters.ClienteId.HasValue && filters.ClienteId > 0)
                        queryParams.Add($"filter[clientes]={filters.ClienteId}");

                    if (filters.CompaniaId.HasValue && filters.CompaniaId > 0)
                        queryParams.Add($"filter[comcod]={filters.CompaniaId}");

                    if (filters.SeccionId.HasValue && filters.SeccionId > 0)
                        queryParams.Add($"filter[seccod]={filters.SeccionId}");

                    if (!string.IsNullOrEmpty(filters.Estado))
                        queryParams.Add($"filter[estado]={Uri.EscapeDataString(filters.Estado)}");

                    if (filters.FechaDesde.HasValue)
                        queryParams.Add($"filter[fecha_desde]={filters.FechaDesde.Value:yyyy-MM-dd}");

                    if (filters.FechaHasta.HasValue)
                        queryParams.Add($"filter[fecha_hasta]={filters.FechaHasta.Value:yyyy-MM-dd}");

                    if (filters.SoloActivos)
                        queryParams.Add("filter[vigencia]=1");
                }

                var url = $"v1/contratos?{string.Join("&", queryParams)}";

                _logger.LogInformation("🔍 Obteniendo pólizas paginadas para tenant - Página: {Page}, Tamaño: {PageSize}", page, pageSize);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoContratoResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                            }
                        });

                    var result = new VelneoPaginatedResponse<ContratoItem>
                    {
                        Items = velneoResponse?.contratos ?? new List<ContratoItem>(),
                        Count = velneoResponse?.count ?? 0,
                        TotalCount = velneoResponse?.total_count ?? 0,
                        Page = page,
                        PageSize = pageSize
                    };

                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));

                    _logger.LogInformation("✅ Pólizas paginadas obtenidas para tenant: {Count}/{TotalCount}",
                        result.Count, result.TotalCount);

                    return result;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo pólizas paginadas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo pólizas paginadas para tenant");
            }

            return new VelneoPaginatedResponse<ContratoItem>();
        }

        public async Task<List<ContratoItem>> SearchPolizasQuickAsync(string numeroPoliza, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(numeroPoliza) || numeroPoliza.Length < 2)
                return new List<ContratoItem>();

            var cacheKey = GetTenantCacheKey($"polizas_quick_search_{numeroPoliza.ToLower()}_{limit}");

            if (_cache.TryGetValue(cacheKey, out List<ContratoItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var queryParams = new List<string>
                {
                    $"api_key={apiKey}",
                    $"filter[poliza]={Uri.EscapeDataString(numeroPoliza)}",
                    $"page[size]={limit}"
                };

                var url = $"v1/contratos?{string.Join("&", queryParams)}";

                _logger.LogInformation("🔍 Búsqueda rápida de pólizas para tenant: '{NumeroPoliza}' (limit: {Limit})", numeroPoliza, limit);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoContratoResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                            }
                        });

                    var polizas = velneoResponse?.contratos ?? new List<ContratoItem>();

                    _cache.Set(cacheKey, polizas, TimeSpan.FromSeconds(30));

                    _logger.LogInformation("✅ Búsqueda rápida de pólizas completada para tenant: {Count} resultados para '{NumeroPoliza}'",
                        polizas.Count, numeroPoliza);

                    return polizas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en búsqueda rápida de pólizas para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción en búsqueda rápida de pólizas para tenant con número '{NumeroPoliza}'", numeroPoliza);
            }

            return new List<ContratoItem>();
        }

        public async Task<ContratoItem?> GetPolizaDetalleAsync(int polizaId)
        {
            var cacheKey = GetTenantCacheKey($"poliza_detalle_{polizaId}");

            if (_cache.TryGetValue(cacheKey, out ContratoItem? cached) && cached != null)
            {
                _logger.LogInformation("💾 Póliza {PolizaId} obtenida desde cache para tenant", polizaId);
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                _logger.LogInformation("📋 Obteniendo detalle póliza para tenant: {PolizaId}", polizaId);

                var response = await client.GetAsync($"v1/contratos/{polizaId}?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                    }
                        };

                        var velneoResponse = JsonSerializer.Deserialize<VelneoContratoResponse>(json, jsonOptions);

                        if (velneoResponse?.contratos?.Count > 0)
                        {
                            var poliza = velneoResponse.contratos[0]; 
                            _cache.Set(cacheKey, poliza, TimeSpan.FromMinutes(15));
                            _logger.LogInformation("✅ Póliza {PolizaId} obtenida para tenant: {NumeroPoliza}",
                                polizaId, poliza.conpol);
                            return poliza;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo póliza {PolizaId} para tenant: {StatusCode} - {Error}",
                        polizaId, response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo póliza {PolizaId} para tenant", polizaId);
            }

            return null;
        }

        public async Task<CreatePolizaResponse> CreatePolizaAsync(VelneoPolizaRequest request)
        {
            try
            {
                // DEBUG: Verificar configuración
                var userId = _tenantService.GetCurrentTenantUserId();
                _logger.LogWarning("🔍 DEBUG CreatePoliza - UserId: {UserId}", userId);

                if (userId == null)
                {
                    _logger.LogError("❌ UserId es NULL - usuario no autenticado");
                    return new CreatePolizaResponse
                    {
                        Success = false,
                        Message = "Usuario no autenticado"
                    };
                }

                var (baseUrl, apiKey) = await GetTenantConfigAsync();
                _logger.LogWarning("🔍 DEBUG CreatePoliza - BaseUrl: {BaseUrl}", baseUrl);
                _logger.LogWarning("🔍 DEBUG CreatePoliza - ApiKey: {ApiKey}", apiKey?.Substring(0, 8) + "...");

                // Verificar el JSON que se va a enviar
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                _logger.LogWarning("🔍 DEBUG CreatePoliza - Request JSON: {Json}", json);

                using var client = await CreateTenantHttpClientAsync();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var fullUrl = $"v1/contratos?api_key={apiKey}";
                _logger.LogWarning("🔍 DEBUG CreatePoliza - Full URL: {BaseUrl}/{FullUrl}", baseUrl, fullUrl);

                var response = await client.PostAsync(fullUrl, content);

                _logger.LogWarning("🔍 DEBUG CreatePoliza - HTTP Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("🔍 DEBUG CreatePoliza - Success Response: {Response}", responseJson);
                    _logger.LogWarning("🔍 DEBUG CreatePoliza - Response Length: {Length}", responseJson?.Length ?? 0);
                    _logger.LogWarning("🔍 DEBUG CreatePoliza - Response is null or empty: {IsEmpty}", string.IsNullOrWhiteSpace(responseJson));

                    // Si la respuesta está vacía, no intentar deserializar
                    if (string.IsNullOrWhiteSpace(responseJson))
                    {
                        _logger.LogError("❌ Velneo devolvió respuesta vacía con HTTP 200");
                        return new CreatePolizaResponse
                        {
                            Success = false,
                            Message = "Velneo devolvió respuesta vacía. Posible problema con la API Key o endpoint."
                        };
                    }

                    // Deserializar como respuesta de contratos de Velneo
                    var velneoResponse = JsonSerializer.Deserialize<VelneoContratoResponse>(responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (velneoResponse?.contratos?.Count > 0)
                    {
                        var polizaCreada = velneoResponse.contratos[0];
                        _logger.LogInformation("✅ Póliza creada exitosamente para tenant: {PolizaId}", polizaCreada.id);

                        return new CreatePolizaResponse
                        {
                            Success = true,
                            VelneoPolizaId = polizaCreada.id,
                            PolizaNumber = polizaCreada.conpol,
                            Message = "Póliza creada exitosamente",
                            PolizaId = polizaCreada.id
                        };
                    }
                    else
                    {
                        _logger.LogError("❌ Velneo no devolvió contratos en la respuesta");
                        return new CreatePolizaResponse
                        {
                            Success = false,
                            Message = "Velneo no devolvió información de la póliza creada"
                        };
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error creando póliza para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);

                    return new CreatePolizaResponse
                    {
                        Success = false,
                        Message = $"Error HTTP {response.StatusCode}: {error}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción creando póliza para tenant");
                return new CreatePolizaResponse
                {
                    Success = false,
                    Message = $"Excepción: {ex.Message}"
                };
            }
        }

        public async Task<VelneoPaginatedResponse<ClienteItem>> GetClientesPaginatedAsync(
    int page = 1,
    int pageSize = 20,
    ClienteSearchFilters? filters = null)
        {
            var cacheKey = GetTenantCacheKey($"clientes_page_{page}_{pageSize}_{filters?.GetCacheKey() ?? "all"}");

            if (_cache.TryGetValue(cacheKey, out VelneoPaginatedResponse<ClienteItem>? cached) && cached != null)
            {
                _logger.LogDebug("💾 Clientes paginados desde cache para tenant - Página: {Page}", page);
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var queryParams = new List<string>
        {
            $"api_key={apiKey}",
            $"page[number]={page}",
            $"page[size]={pageSize}"
        };

                // Aplicar filtros si existen
                if (filters != null)
                {
                    if (!string.IsNullOrEmpty(filters.Nombre))
                        queryParams.Add($"filter[nombre]={Uri.EscapeDataString(filters.Nombre)}");

                    if (!string.IsNullOrEmpty(filters.Cliced))
                        queryParams.Add($"filter[documento]={Uri.EscapeDataString(filters.Cliced)}");

                    if (!string.IsNullOrEmpty(filters.Clicel))
                        queryParams.Add($"filter[clicel]={Uri.EscapeDataString(filters.Clicel)}");

                    if (!string.IsNullOrEmpty(filters.Clitel))
                        queryParams.Add($"filter[clitel]={Uri.EscapeDataString(filters.Clitel)}");

                    if (!string.IsNullOrEmpty(filters.Mail))
                        queryParams.Add($"filter[mail]={Uri.EscapeDataString(filters.Mail)}");

                    if (!string.IsNullOrEmpty(filters.Cliruc))
                        queryParams.Add($"filter[cliruc]={Uri.EscapeDataString(filters.Cliruc)}");

                    if (filters.SoloActivos)
                        queryParams.Add("filter[activo]=true");
                }

                var url = $"v1/clientes?{string.Join("&", queryParams)}";

                _logger.LogInformation("🔍 Obteniendo clientes paginados para tenant - Página: {Page}, Tamaño: {PageSize}", page, pageSize);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                            }
                        });

                    var result = new VelneoPaginatedResponse<ClienteItem>
                    {
                        Items = velneoResponse?.clientes ?? new List<ClienteItem>(),
                        Count = velneoResponse?.count ?? 0,
                        TotalCount = velneoResponse?.total_count ?? 0,
                        Page = page,
                        PageSize = pageSize
                    };

                    // Cache más corto para páginas paginadas (2 minutos)
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));

                    _logger.LogInformation("✅ Clientes paginados obtenidos para tenant: {Count}/{TotalCount}",
                        result.Count, result.TotalCount);

                    return result;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo clientes paginados para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo clientes paginados para tenant");
            }

            return new VelneoPaginatedResponse<ClienteItem>();
        }

        /// <summary>
        /// Búsqueda rápida de clientes por nombre (para autocomplete/typeahead)
        /// </summary>
        public async Task<List<ClienteItem>> SearchClientesQuickAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<ClienteItem>();

            var cacheKey = GetTenantCacheKey($"clientes_quick_search_{query.ToLower()}_{limit}");

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                var queryParams = new List<string>
        {
            $"api_key={apiKey}",
            $"filter[nombre]={Uri.EscapeDataString(query)}",
            $"page[size]={limit}",
            "filter[activo]=true"
        };

                var url = $"v1/clientes?{string.Join("&", queryParams)}";

                _logger.LogInformation("🔍 Búsqueda rápida de clientes para tenant: '{Query}' (limit: {Limit})", query, limit);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                            }
                        });

                    var clientes = velneoResponse?.clientes ?? new List<ClienteItem>();

                    // Cache corto para búsquedas rápidas (1 minuto)
                    _cache.Set(cacheKey, clientes, TimeSpan.FromMinutes(1));

                    _logger.LogInformation("✅ Búsqueda rápida completada para tenant: {Count} resultados para '{Query}'",
                        clientes.Count, query);

                    return clientes;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en búsqueda rápida para tenant: {StatusCode} - {Error}",
                        response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción en búsqueda rápida de clientes para tenant con query '{Query}'", query);
            }

            return new List<ClienteItem>();
        }

        /// <summary>
        /// Obtiene detalle de un cliente específico (mantenido igual)
        /// </summary>
        public async Task<ClienteItem?> GetClienteDetalleAsync(int clienteId)
        {
            var cacheKey = GetTenantCacheKey($"cliente_detalle_{clienteId}");

            if (_cache.TryGetValue(cacheKey, out ClienteItem? cached) && cached != null)
            {
                _logger.LogInformation("💾 Cliente {ClienteId} obtenido desde cache para tenant", clienteId);
                return cached;
            }

            try
            {
                using var client = await CreateTenantHttpClientAsync();
                var (_, apiKey) = await GetTenantConfigAsync();

                _logger.LogInformation("👤 Obteniendo detalle cliente para tenant: {ClienteId}", clienteId);

                var response = await client.GetAsync($"v1/clientes/{clienteId}?api_key={apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = {
                        new NullableDateTimeConverter(),
                        new DateTimeConverter()
                    }
                        };

                        var cliente = JsonSerializer.Deserialize<ClienteItem>(json, jsonOptions);

                        if (cliente != null)
                        {
                            // Cache más largo para detalles específicos (30 minutos)
                            _cache.Set(cacheKey, cliente, TimeSpan.FromMinutes(30));
                            _logger.LogInformation("✅ Cliente {ClienteId} obtenido para tenant: {Nombre}",
                                clienteId, cliente.clinom);
                            return cliente;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error obteniendo cliente {ClienteId} para tenant: {StatusCode} - {Error}",
                        clienteId, response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción obteniendo cliente {ClienteId} para tenant", clienteId);
            }

            return null;
        }

    }
}