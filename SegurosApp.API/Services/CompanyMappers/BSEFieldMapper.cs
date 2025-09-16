using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.CompanyMappers
{
    /// <summary>
    /// Mapper para BSE - comportamiento base y estándar.
    /// Este mapper mantiene la lógica actual que funciona bien para BSE.
    /// </summary>
    public class BSEFieldMapper : ICompanyFieldMapper
    {
        public readonly ILogger<BSEFieldMapper> _logger;

        public BSEFieldMapper(ILogger<BSEFieldMapper> logger)
        {
            _logger = logger;
        }

        public virtual string GetCompanyName() => "BSE";

        public virtual async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            _logger.LogDebug("Normalizando campos BSE (formato estándar)");

            var normalized = new Dictionary<string, object>(extractedData);
            MapStandardFields(normalized);

            return await Task.FromResult(normalized);
        }

        public virtual async Task<int> MapCombustibleAsync(Dictionary<string, object> data, List<CombustibleItem> combustibles)
        {
            var combustibleText = GetFieldValue(data, "vehiculo.combustible", "COMBUSTIBLE");
            return MapCombustibleByText(combustibleText, combustibles);
        }

        public virtual async Task<int> MapDestinoAsync(Dictionary<string, object> data, List<DestinoItem> destinos)
        {
            var destinoText = GetFieldValue(data, "vehiculo.destino_del_vehiculo", "DESTINO DEL VEHÍCULO");
            return MapDestinoByText(destinoText, destinos);
        }

        public virtual async Task<int> MapDepartamentoAsync(Dictionary<string, object> data, List<DepartamentoItem> departamentos)
        {
            var deptoText = GetFieldValue(data, "asegurado.departamento", "asegurado.localidad");
            return MapDepartamentoByText(deptoText, departamentos);
        }

        public virtual async Task<int> MapCalidadAsync(Dictionary<string, object> data, List<CalidadItem> calidades)
        {
            var calidadText = GetFieldValue(data, "vehiculo.calidad_de_contratante", "CALIDAD DE CONTRATANTE");
            return MapCalidadByText(calidadText, calidades);
        }

        public virtual async Task<int> MapCategoriaAsync(Dictionary<string, object> data, List<CategoriaItem> categorias)
        {
            var categoriaText = GetFieldValue(data, "vehiculo.tipo_de_vehiculo", "vehiculo.tipo");
            return MapCategoriaByText(categoriaText, categorias);
        }

        public virtual async Task<int> MapTarifaAsync(Dictionary<string, object> data, List<TarifaItem> tarifas)
        {
            var modalidadText = GetFieldValue(data, "poliza.modalidad", "vehiculo.modalidad");
            return MapTarifaByText(modalidadText, tarifas);
        }

        #region Métodos Auxiliares Protegidos

        /// <summary>
        /// Mapea campos estándar de BSE al formato interno normalizado
        /// </summary>
        protected virtual void MapStandardFields(Dictionary<string, object> data)
        {
            // Campos de póliza
            MapField(data, "poliza.numero", "numero_poliza");
            MapField(data, "poliza.vigencia.desde", "fecha_desde");
            MapField(data, "poliza.vigencia.hasta", "fecha_hasta");
            MapField(data, "poliza.prima_comercial", "prima_total");
            MapField(data, "financiero.prima_comercial", "prima_comercial");
            MapField(data, "financiero.premio_total", "premio_total");

            // Campos de vehículo
            MapField(data, "vehiculo.marca", "vehiculo_marca");
            MapField(data, "vehiculo.modelo", "vehiculo_modelo");
            MapField(data, "vehiculo.anio", "vehiculo_anio");
            MapField(data, "vehiculo.matricula", "vehiculo_matricula");
            MapField(data, "vehiculo.chasis", "vehiculo_chasis");
            MapField(data, "vehiculo.motor", "vehiculo_motor");

            // Campos de asegurado
            MapField(data, "asegurado.nombre", "asegurado_nombre");
            MapField(data, "asegurado.direccion", "asegurado_direccion");
        }

        /// <summary>
        /// Obtiene el valor de un campo buscando en múltiples keys posibles
        /// </summary>
        protected string GetFieldValue(Dictionary<string, object> data, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (data.TryGetValue(key, out var value) && value != null)
                {
                    return CleanFieldValue(value.ToString());
                }
            }
            return "";
        }

        /// <summary>
        /// Limpia valores de campo removiendo caracteres no deseados
        /// </summary>
        protected string CleanFieldValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\n", " ")
                       .Replace(":", "")
                       .Trim();
        }

        /// <summary>
        /// Mapea un campo de origen a destino si existe
        /// </summary>
        protected void MapField(Dictionary<string, object> data, string sourceKey, string targetKey)
        {
            if (data.TryGetValue(sourceKey, out var value) && value != null)
            {
                data[targetKey] = value;
            }
        }

        #endregion

        #region Métodos de Mapeo por Tipo

        protected int MapCombustibleByText(string text, List<CombustibleItem> combustibles)
        {
            var combustibleMappings = new Dictionary<string, string[]>
            {
                { "GAS", new[] { "NAFTA", "GASOLINA", "SUPER" } },
                { "DIS", new[] { "DIESEL", "DISEL", "GAS-OIL", "GASOIL" } },
                { "ELE", new[] { "ELECTRICO", "ELÉCTRICO" } },
                { "HYB", new[] { "HIBRIDO", "HÍBRIDO", "HYBRID" } }
            };

            return FindBestMatch(text, combustibles.Select(c => c.ToMasterItem()).ToList(), combustibleMappings);
        }

        protected int MapDestinoByText(string text, List<DestinoItem> destinos)
        {
            var destinoMappings = new Dictionary<string, string[]>
            {
                { "PARTICULAR", new[] { "PARTICULAR", "PERSONAL", "PRIVADO" } },
                { "COMERCIAL", new[] { "COMERCIAL", "TRABAJO", "LABORAL" } },
                { "TAXI", new[] { "TAXI", "REMISE" } },
                { "CARGA", new[] { "CARGA", "TRANSPORTE" } }
            };

            return FindBestMatch(text, destinos.Select(d => d.ToMasterItem()).ToList(), destinoMappings);
        }

        protected int MapDepartamentoByText(string text, List<DepartamentoItem> departamentos)
        {
            return FindBestMatchByName(text, departamentos.Select(d => d.ToMasterItem()).ToList());
        }

        protected int MapCalidadByText(string text, List<CalidadItem> calidades)
        {
            var calidadMappings = new Dictionary<string, string[]>
            {
                { "PROPIETARIO", new[] { "PROPIETARIO", "DUEÑO", "OWNER" } },
                { "USUFRUCTUARIO", new[] { "USUFRUCTUARIO", "USUARIO" } },
                { "LOCATARIO", new[] { "LOCATARIO", "INQUILINO" } }
            };

            return FindBestMatch(text, calidades.Select(c => c.ToMasterItem()).ToList(), calidadMappings);
        }

        protected int MapCategoriaByText(string text, List<CategoriaItem> categorias)
        {
            return FindBestMatchByName(text, categorias.Select(c => c.ToMasterItem()).ToList());
        }

        protected int MapTarifaByText(string text, List<TarifaItem> tarifas)
        {
            // Si no hay texto específico, buscar tarifa por defecto
            if (string.IsNullOrEmpty(text))
            {
                var defaultTarifa = tarifas.FirstOrDefault(t =>
                    t.tarnom.ToUpper().Contains("ESTANDAR") ||
                    t.tarnom.ToUpper().Contains("GENERAL") ||
                    t.tarnom.ToUpper().Contains("BASICA") ||
                    t.tarnom.ToUpper().Contains("TRIPLE"));

                if (defaultTarifa != null)
                {
                    _logger.LogDebug("Usando tarifa por defecto: {Tarifa}", defaultTarifa.tarnom);
                    return defaultTarifa.id;
                }

                return tarifas.FirstOrDefault()?.id ?? 1;
            }

            return FindBestMatchByName(text, tarifas.Select(t => t.ToMasterItem()).ToList());
        }

        #endregion

        #region Algoritmos de Matching

        /// <summary>
        /// Encuentra la mejor coincidencia usando mapeos específicos primero, luego similitud
        /// </summary>
        protected int FindBestMatch(string text, List<IVelneoMasterItem> items, Dictionary<string, string[]> mappings)
        {
            if (string.IsNullOrEmpty(text) || !items.Any())
                return items.FirstOrDefault()?.Id ?? 1;

            var normalizedText = text.ToUpper();
            _logger.LogDebug("Buscando coincidencia para: '{Text}'", text);

            // Buscar por mapeos específicos primero
            foreach (var mapping in mappings)
            {
                if (mapping.Value.Any(alias => normalizedText.Contains(alias)))
                {
                    var item = items.FirstOrDefault(i =>
                        i.Codigo?.ToUpper() == mapping.Key ||
                        i.Nombre?.ToUpper().Contains(mapping.Key) == true);

                    if (item != null)
                    {
                        _logger.LogDebug("Mapeo directo encontrado: '{Text}' -> '{Item}' via {Key}",
                            text, item.Nombre, mapping.Key);
                        return item.Id;
                    }
                }
            }

            // Fallback a búsqueda por similitud
            return FindBestMatchByName(text, items);
        }

        /// <summary>
        /// Encuentra la mejor coincidencia por similitud de nombre
        /// </summary>
        protected int FindBestMatchByName(string text, List<IVelneoMasterItem> items)
        {
            if (string.IsNullOrEmpty(text) || !items.Any())
                return items.FirstOrDefault()?.Id ?? 1;

            var normalizedText = text.ToUpper().Replace("\n", " ").Trim();

            // Búsqueda exacta primero
            var exact = items.FirstOrDefault(i =>
                i.Nombre?.ToUpper().Contains(normalizedText) == true ||
                normalizedText.Contains(i.Nombre?.ToUpper() ?? ""));

            if (exact != null)
            {
                _logger.LogDebug("Coincidencia exacta: '{Text}' -> '{Item}'", text, exact.Nombre);
                return exact.Id;
            }

            // Búsqueda por similitud
            var bestMatch = items
                .Select(i => new { Item = i, Score = CalculateSimilarity(normalizedText, i.Nombre ?? "") })
                .Where(x => x.Score > 0.5)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                _logger.LogDebug("Mejor similitud: '{Text}' -> '{Item}' ({Score:P1})",
                    text, bestMatch.Item.Nombre, bestMatch.Score);
                return bestMatch.Item.Id;
            }

            // Último recurso - primer item
            var fallback = items.First();
            _logger.LogDebug("Usando fallback: '{Text}' -> '{Item}'", text, fallback.Nombre);
            return fallback.Id;
        }

        /// <summary>
        /// Calcula similitud entre dos strings basado en palabras comunes
        /// </summary>
        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            text1 = text1.ToUpper();
            text2 = text2.ToUpper();

            // Dividir en palabras y filtrar palabras cortas
            var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length > 2).ToArray();
            var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length > 2).ToArray();

            if (!words1.Any() || !words2.Any())
                return 0;

            // Contar palabras en común (incluyendo coincidencias parciales)
            var commonWords = words1.Count(w1 =>
                words2.Any(w2 => w2.Contains(w1) || w1.Contains(w2)));

            var totalWords = Math.Max(words1.Length, words2.Length);
            return totalWords > 0 ? (double)commonWords / totalWords : 0;
        }

        #endregion
    }
}