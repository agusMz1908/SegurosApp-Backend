using Microsoft.Extensions.Caching.Memory;
using SegurosApp.API.Converters;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Converters;
using SegurosApp.API.Interfaces;
using System.Text;
using System.Text.Json;
using SegurosApp.API.DTOs;

namespace SegurosApp.API.Services
{
    public class VelneoMasterDataService : IVelneoMasterDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<VelneoMasterDataService> _logger;

        private string ApiKey => _configuration["VelneoAPI:ApiKey"];
        private string BaseUrl => _configuration["VelneoAPI:BaseUrl"];

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

        public async Task<List<CompaniaItem>> GetCompaniasAsync()
        {
            const string cacheKey = "velneo_companias";

            if (_cache.TryGetValue(cacheKey, out List<CompaniaItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🏢 Obteniendo compañías desde Velneo...");

                // ✅ LLAMADA REAL A VELNEO - Endpoint que funciona
                var url = $"{BaseUrl}/companias?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("🏢 Raw response from Velneo companias: {Json}",
                        json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                    var velneoResponse = JsonSerializer.Deserialize<VelneoCompaniaResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (velneoResponse?.companias != null)
                    {
                        // ✅ FILTRAR SOLO COMPAÑÍAS ACTIVAS (que tienen nombre)
                        var companiasActivas = velneoResponse.companias
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.DisplayName)
                            .ToList();

                        // Cache por 4 horas (datos maestros cambian poco)
                        _cache.Set(cacheKey, companiasActivas, TimeSpan.FromHours(4));

                        _logger.LogInformation("✅ Compañías obtenidas desde Velneo: {Count}/{Total} activas",
                            companiasActivas.Count, velneoResponse.companias.Count);
                        return companiasActivas;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo compañías vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo companias: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo compañías desde Velneo");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON compañías de Velneo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo compañías desde Velneo");
            }

            return new List<CompaniaItem>();
        }

        /// <summary>
        /// 📋 Obtener secciones reales desde API Velneo
        /// </summary>
        public async Task<List<SeccionItem>> GetSeccionesAsync(int? companiaId = null)
        {
            var cacheKey = companiaId.HasValue
                ? $"velneo_secciones_compania_{companiaId}"
                : "velneo_secciones_all";

            if (_cache.TryGetValue(cacheKey, out List<SeccionItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("📋 Obteniendo secciones desde Velneo (compañía: {CompaniaId})...",
                    companiaId?.ToString() ?? "todas");

                // Nota: Por ahora todas las secciones, luego se puede filtrar por compañía
                var url = $"{BaseUrl}/secciones?api_key={ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("📋 Raw response from Velneo secciones: {Json}",
                        json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                    var velneoResponse = JsonSerializer.Deserialize<VelneoSeccionResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (velneoResponse?.secciones != null)
                    {
                        // ✅ FILTRAR SOLO SECCIONES ACTIVAS
                        var seccionesActivas = velneoResponse.secciones
                            .Where(s => s.IsActive)
                            .OrderBy(s => s.DisplayName)
                            .ToList();

                        // TODO: Cuando Velneo implemente filtro por compañía, usar companiaId
                        // Por ahora devolvemos todas las secciones activas

                        // Cache por 4 horas
                        _cache.Set(cacheKey, seccionesActivas, TimeSpan.FromHours(4));

                        _logger.LogInformation("✅ Secciones obtenidas desde Velneo: {Count}/{Total} activas",
                            seccionesActivas.Count, velneoResponse.secciones.Count);
                        return seccionesActivas;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo secciones vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo secciones: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo secciones desde Velneo");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON secciones de Velneo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo secciones desde Velneo");
            }

            return new List<SeccionItem>();
        }

        public async Task<List<ClienteItem>> SearchClientesAsync(string query, int limit = 20)
        {
            var cacheKey = $"velneo_clientes_search_{query.ToLowerInvariant()}_{limit}";

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🔍 Buscando clientes en Velneo con filtro: '{Query}' (limit: {Limit})", query, limit);

                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{BaseUrl}/clientes?filter%5Bnombre%5D={encodedQuery}&api_key={ApiKey}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = {
                    new NullableDateTimeConverter(),
                    new DateTimeConverter()
                }
                    };

                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json, jsonOptions);

                    if (velneoResponse?.clientes != null)
                    {
                        var clientesFiltrados = velneoResponse.clientes
                            .Where(c => c.activo)
                            .Take(limit)
                            .ToList();

                        _cache.Set(cacheKey, clientesFiltrados, TimeSpan.FromMinutes(5));

                        _logger.LogInformation("✅ Clientes encontrados en Velneo: {Count}/{Total} activos para '{Query}'",
                            clientesFiltrados.Count, velneoResponse.clientes.Count, query);

                        return clientesFiltrados;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo clientes vacía o malformada para query '{Query}'", query);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo clientes con filtro: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON de clientes Velneo para query '{Query}' - Path: {Path}",
                    query, ex.Path ?? "desconocido");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión buscando clientes en Velneo con query '{Query}'", query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado buscando clientes en Velneo con query '{Query}'", query);
            }

            return new List<ClienteItem>();
        }

        public async Task<List<ClienteItem>> AdvancedSearchClientesAsync(ClienteSearchFilters filters)
        {
            var cacheKey = $"velneo_clientes_advanced_{filters.GetCacheKey()}";

            if (_cache.TryGetValue(cacheKey, out List<ClienteItem>? cached) && cached != null)
                return cached;

            try
            {
                _logger.LogInformation("🔍 Búsqueda avanzada clientes en Velneo: {Filters}", filters.ToString());

                var urlBuilder = new StringBuilder($"{BaseUrl}/clientes?");
                var hasFilters = false;
                if (!string.IsNullOrWhiteSpace(filters.Nombre))
                {
                    urlBuilder.Append($"filter%5Bnombre%5D={Uri.EscapeDataString(filters.Nombre)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Direcciones))
                {
                    urlBuilder.Append($"filter%5Bdirecciones%5D={Uri.EscapeDataString(filters.Direcciones)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Clitel))
                {
                    urlBuilder.Append($"filter%5Bclitel%5D={Uri.EscapeDataString(filters.Clitel)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Clicel))
                {
                    urlBuilder.Append($"filter%5Bclicel%5D={Uri.EscapeDataString(filters.Clicel)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Mail))
                {
                    urlBuilder.Append($"filter%5Bmail%5D={Uri.EscapeDataString(filters.Mail)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Cliruc))
                {
                    urlBuilder.Append($"filter%5Bcliruc%5D={Uri.EscapeDataString(filters.Cliruc)}&");
                    hasFilters = true;
                }

                if (!string.IsNullOrWhiteSpace(filters.Cliced))
                {
                    urlBuilder.Append($"filter%5Bcliced%5D={Uri.EscapeDataString(filters.Cliced)}&");
                    hasFilters = true;
                }

                urlBuilder.Append($"api_key={ApiKey}");

                if (!hasFilters)
                {
                    _logger.LogWarning("⚠️ Búsqueda avanzada sin filtros - esto puede devolver muchos resultados");
                }

                var url = urlBuilder.ToString();
                _logger.LogDebug("🔍 URL Velneo búsqueda avanzada: {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("🔍 Raw response Velneo búsqueda avanzada (primeros 300 chars): {Json}",
                        json.Length > 300 ? json.Substring(0, 300) + "..." : json);

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = {
                    new NullableDateTimeConverter(),
                    new DateTimeConverter()
                }
                    };

                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json, jsonOptions);

                    if (velneoResponse?.clientes != null)
                    {
                        var clientesFiltrados = velneoResponse.clientes.AsEnumerable();
                        if (filters.SoloActivos)
                        {
                            clientesFiltrados = clientesFiltrados.Where(c => c.activo);
                        }

                        var resultados = clientesFiltrados.Take(filters.Limit).ToList();

                        _cache.Set(cacheKey, resultados, TimeSpan.FromMinutes(3));

                        _logger.LogInformation("✅ Búsqueda avanzada clientes completada: {Count}/{Total} resultados con {ActiveFilters} filtros activos",
                            resultados.Count, velneoResponse.clientes.Count, filters.GetActiveFiltersCount());

                        if (resultados.Count > 0)
                        {
                            var ejemplo = resultados.First();
                            _logger.LogDebug("📋 Ejemplo resultado: ID={Id}, Nombre='{Nombre}', RUC='{Ruc}', Cédula='{Cedula}'",
                                ejemplo.id, ejemplo.DisplayName, ejemplo.cliruc, ejemplo.cliced);
                        }

                        return resultados;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo búsqueda avanzada vacía o malformada");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo búsqueda avanzada: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "❌ Error construyendo URL para búsqueda avanzada");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión en búsqueda avanzada clientes");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error parseando JSON búsqueda avanzada clientes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado en búsqueda avanzada clientes");
            }

            return new List<ClienteItem>();
        }

        public async Task<ClienteItem?> GetClienteDetalleAsync(int clienteId)
        {
            var cacheKey = $"velneo_cliente_detalle_{clienteId}";

            if (_cache.TryGetValue(cacheKey, out ClienteItem? cached) && cached != null)
            {
                _logger.LogInformation("💾 Cliente {ClienteId} obtenido desde cache", clienteId);
                return cached;
            }

            try
            {
                _logger.LogInformation("👤 Obteniendo detalle cliente desde Velneo: {ClienteId}", clienteId);

                var url = $"{BaseUrl}/clientes/{clienteId}?api_key={ApiKey}";
                _logger.LogDebug("🔗 URL Velneo: {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("📄 Raw JSON response length: {Length} characters", json.Length);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("⚠️ Velneo devolvió respuesta vacía para cliente {ClienteId}", clienteId);
                        return null;
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = {
                    new NullableDateTimeConverter(),
                    new DateTimeConverter()
                        }
                    };

                    var velneoResponse = JsonSerializer.Deserialize<VelneoClienteDetalleResponse>(json, jsonOptions);

                    if (velneoResponse?.clientes != null && velneoResponse.clientes.Count > 0)
                    {
                        var cliente = velneoResponse.clientes.First();

                        _logger.LogInformation("✅ Cliente deserializado exitosamente: ID={Id}, Nombre='{Nombre}', RUC='{Ruc}', Activo={Activo}",
                            cliente.id, cliente.clinom, cliente.cliruc, cliente.activo);

                        LogDateTimeFields(cliente, clienteId);

                        if (cliente.id > 0 && !string.IsNullOrEmpty(cliente.clinom))
                        {
                            _cache.Set(cacheKey, cliente, TimeSpan.FromHours(1));

                            _logger.LogInformation("✅ Cliente {ClienteId} cacheado exitosamente - {DisplayName}",
                                clienteId, cliente.DisplayName);

                            return cliente;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Cliente {ClienteId} encontrado pero sin datos válidos (ID={Id}, Nombre='{Nombre}')",
                                clienteId, cliente.id, cliente.clinom);
                        }
                    }
                    else if (velneoResponse?.count == 0)
                    {
                        _logger.LogInformation("🔍 Cliente {ClienteId} no existe en Velneo (count=0)", clienteId);
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Respuesta Velneo malformada para cliente {ClienteId}: count={Count}, clientes={ClientesCount}",
                            clienteId, velneoResponse?.count ?? -1, velneoResponse?.clientes?.Count ?? -1);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("🔍 Cliente {ClienteId} no existe en Velneo (404)", clienteId);
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Error en Velneo cliente/{id}: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conexión obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Error deserializando JSON para cliente {ClienteId} desde Velneo", clienteId);

                _logger.LogDebug("🔍 JSON problemático para cliente {ClienteId}: {JsonSnippet}",
                    clienteId, ex.Path ?? "path desconocido");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "❌ Timeout obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo cliente {ClienteId} desde Velneo", clienteId);
            }

            return null;
        }

        private void LogDateTimeFields(ClienteItem cliente, int clienteId)
        {
            try
            {
                var dateFields = new[]
                {
            (nameof(cliente.clifchnac), cliente.clifchnac),
            (nameof(cliente.clifching), cliente.clifching),
            (nameof(cliente.clifchegr), cliente.clifchegr),
            (nameof(cliente.clivtoced), cliente.clivtoced),
            (nameof(cliente.clivtolib), cliente.clivtolib),
            (nameof(cliente.clifchnac1), cliente.clifchnac1),
            (nameof(cliente.fch_ingreso), cliente.fch_ingreso),
            (nameof(cliente.last_update), cliente.last_update)
        };

                var validDates = dateFields.Where(d => d.Item2.HasValue).Count();
                var totalDates = dateFields.Length;

                _logger.LogDebug("📅 Cliente {ClienteId} fechas procesadas: {ValidDates}/{TotalDates} válidas",
                    clienteId, validDates, totalDates);

                foreach (var (fieldName, value) in dateFields.Where(d => d.Item2.HasValue))
                {
                    _logger.LogDebug("📅 {FieldName}: {Value}", fieldName, value!.Value.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error logging fechas para cliente {ClienteId}: {Error}", clienteId, ex.Message);
            }
        }

        public async Task<CompleteMasterDataResponse> GetAllMasterDataAsync()
        {
            const string cacheKey = "velneo_all_master_data_v3"; 

            if (_cache.TryGetValue(cacheKey, out CompleteMasterDataResponse? cached) && cached != null)
                return cached;

            _logger.LogInformation("🔄 Obteniendo master data completo desde Velneo (incluye clientes/compañías/secciones reales)...");

            try
            {
                var departamentosTask = GetDepartamentosAsync();
                var combustiblesTask = GetCombustiblesAsync();
                var corredoresTask = GetCorredoresAsync();
                var categoriasTask = GetCategoriasAsync();
                var destinosTask = GetDestinosAsync();
                var calidadesTask = GetCalidadesAsync();
                var tarifasTask = GetTarifasAsync();
                var companiasTask = GetCompaniasAsync();
                var seccionesTask = GetSeccionesAsync();

                await Task.WhenAll(
                    departamentosTask, combustiblesTask, corredoresTask,
                    categoriasTask, destinosTask, calidadesTask, tarifasTask,
                    companiasTask, seccionesTask
                );

                var result = new CompleteMasterDataResponse
                {
                    Departamentos = await departamentosTask,
                    Combustibles = await combustiblesTask,
                    Corredores = await corredoresTask,
                    Categorias = await categoriasTask,
                    Destinos = await destinosTask,
                    Calidades = await calidadesTask,
                    Tarifas = await tarifasTask,
                    Companias = await companiasTask,
                    Secciones = await seccionesTask,

                    // Datos estáticos
                    EstadosGestion = GetEstadosGestion(),
                    Tramites = GetTramites(),
                    EstadosPoliza = GetEstadosPoliza(),
                    FormasPago = GetFormasPago()
                };

                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

                _logger.LogInformation("✅ Master data completo obtenido desde Velneo: {CompaniasCount} compañías, {SeccionesCount} secciones",
                    result.Companias.Count, result.Secciones.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo desde Velneo");

                return new CompleteMasterDataResponse
                {
                    Departamentos = new(),
                    Combustibles = new(),
                    Corredores = new(),
                    Categorias = new(),
                    Destinos = new(),
                    Calidades = new(),
                    Tarifas = new(),
                    Companias = new(),
                    Secciones = new(),
                    EstadosGestion = GetEstadosGestion(),
                    Tramites = GetTramites(),
                    EstadosPoliza = GetEstadosPoliza(),
                    FormasPago = GetFormasPago()
                };
            }
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
