using System.Text;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.Poliza.Shared
{
    public class ObservationsGenerator
    {
        private readonly ILogger<ObservationsGenerator> _logger;

        public ObservationsGenerator(ILogger<ObservationsGenerator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Genera observaciones para nueva póliza
        /// </summary>
        public string GenerateNewPolizaObservations(
            string? userNotes,
            string? userComments,
            int cuotas,
            int montoTotal,
            Dictionary<string, object> normalizedData)
        {
            var parts = new List<string> { "Generado desde escaneo automático." };
            parts.Add("");

            if (!string.IsNullOrWhiteSpace(userNotes))
                parts.Add($"Notas: {userNotes}");

            if (!string.IsNullOrWhiteSpace(userComments))
                parts.Add($"Comentarios: {userComments}");

            if (cuotas > 1 && montoTotal > 0)
            {
                var cronograma = GenerateInstallmentScheduleFromData(cuotas, montoTotal, normalizedData);
                parts.Add(cronograma);
            }

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Genera observaciones para renovación de póliza
        /// </summary>
        public string GenerateRenewPolizaObservations(
            string polizaAnteriorNumero,
            int polizaAnteriorId,
            int cuotas,
            int montoTotal,
            string fechaDesde,
            DateTime? fechaVencimientoAnterior = null,
            string? observacionesUsuario = null,
            string? comentariosUsuario = null)
        {
            var observations = new List<string>();

            // Header
            observations.Add($"Renovación de Póliza {polizaAnteriorNumero} (ID: {polizaAnteriorId})");

            if (fechaVencimientoAnterior.HasValue)
            {
                observations.Add($"Vencimiento anterior: {fechaVencimientoAnterior.Value:dd/MM/yyyy}");
            }

            observations.Add(""); // Línea en blanco

            // Cronograma de cuotas
            if (cuotas > 1 && montoTotal > 0)
            {
                observations.Add("CRONOGRAMA DE CUOTAS:");
                observations.Add($"Total: ${montoTotal:N2} en {cuotas} cuotas");
                observations.Add("");

                var valorCuota = Math.Round((decimal)montoTotal / cuotas, 2);
                var fechaBase = DateTime.TryParse(fechaDesde, out var fechaInicio) ? fechaInicio : DateTime.Now;

                for (int i = 1; i <= cuotas; i++)
                {
                    var fechaCuota = fechaBase.AddMonths(i - 1);
                    var montoCuota = (i == cuotas)
                        ? (decimal)montoTotal - (valorCuota * (cuotas - 1))
                        : valorCuota;

                    observations.Add($"Cuota {i:00}: {fechaCuota:dd/MM/yyyy} - ${montoCuota:N2}");
                }

                observations.Add("");
                observations.Add("=== FIN CRONOGRAMA ===");
            }
            else if (cuotas == 1)
            {
                observations.Add($"Pago contado: ${montoTotal:N2}");
            }

            // ✅ UNIFICAR OBSERVACIONES Y COMENTARIOS EN UNA SOLA SECCIÓN
            var notasUsuario = CombineUserNotes(observacionesUsuario, comentariosUsuario);
            if (!string.IsNullOrEmpty(notasUsuario))
            {
                observations.Add("");
                observations.Add("OBSERVACIONES ADICIONALES:");
                observations.Add(notasUsuario);
            }

            return string.Join("\n", observations);
        }

        private string CombineUserNotes(string? observacionesUsuario, string? comentariosUsuario)
        {
            var notes = new List<string>();

            if (!string.IsNullOrEmpty(observacionesUsuario) &&
                !observacionesUsuario.Contains("Renovación automática"))
            {
                notes.Add(observacionesUsuario);
            }

            if (!string.IsNullOrEmpty(comentariosUsuario))
            {
                notes.Add(comentariosUsuario);
            }

            return string.Join("\n", notes);
        }

        /// <summary>
        /// Genera observaciones para cambio/modificación de póliza
        /// </summary>
        public string GenerateModifyPolizaObservations(
                    string polizaAnteriorNumero,
                    int polizaAnteriorId,
                    string tipoCambio,
                    int cuotas,
                    int montoTotal,
                    string fechaDesde,
                    string? observacionesUsuario = null,
                    string? comentariosUsuario = null,
                    Dictionary<string, string>? cambiosDetectados = null)
        {
            var observations = new List<string>();

            // ✅ HEADER PRINCIPAL
            observations.Add($"Cambio de Poliza {polizaAnteriorNumero} (ID: {polizaAnteriorId})");
            observations.Add($"Tipo de cambio: {tipoCambio}");

            // ✅ CAMBIOS DETECTADOS
            if (cambiosDetectados != null && cambiosDetectados.Count > 0)
            {
                observations.Add(""); // Línea en blanco
                observations.Add("CAMBIOS REALIZADOS:");

                foreach (var cambio in cambiosDetectados)
                {
                    observations.Add($"- {cambio.Key}: {cambio.Value}");
                }
            }

            // ✅ CRONOGRAMA DE CUOTAS
            if (cuotas > 1 && montoTotal > 0)
            {
                observations.Add(""); // Línea en blanco
                observations.Add("CRONOGRAMA DE CUOTAS:");

                var valorCuota = Math.Round((decimal)montoTotal / cuotas, 2);
                var fechaBase = DateTime.TryParse(fechaDesde, out var fechaInicio) ? fechaInicio : DateTime.Now;

                for (int i = 1; i <= cuotas; i++)
                {
                    var fechaCuota = fechaBase.AddMonths(i - 1);
                    var montoCuota = (i == cuotas)
                        ? (decimal)montoTotal - (valorCuota * (cuotas - 1)) // Última cuota ajusta diferencia
                        : valorCuota;

                    observations.Add($"Cuota {i:00}: {fechaCuota:dd/MM/yyyy} - ${montoCuota:N2}");
                }

                observations.Add($"TOTAL: ${montoTotal:N2} en {cuotas} cuotas");
            }
            else if (cuotas == 1)
            {
                observations.Add($"Pago contado: ${montoTotal:N2}");
            }

            // ✅ UNIFICAR OBSERVACIONES Y COMENTARIOS DEL USUARIO
            var notasUsuario = CombineUserNotes(observacionesUsuario, comentariosUsuario);
            if (!string.IsNullOrEmpty(notasUsuario))
            {
                observations.Add(""); // Línea en blanco
                observations.Add("OBSERVACIONES ADICIONALES:");
                observations.Add(notasUsuario);
            }

            var result = string.Join("\n", observations);

            _logger.LogInformation("Observaciones generadas para cambio - Póliza: {PolizaAnteriorNumero}, Tipo: {TipoCambio}, Caracteres: {Length}",
                polizaAnteriorNumero, tipoCambio, result.Length);

            return result;
        }

        /// <summary>
        /// Genera cronograma de cuotas usando datos normalizados del escaneo
        /// </summary>
        private string GenerateInstallmentScheduleFromData(
            int cuotas,
            int montoTotal,
            Dictionary<string, object> normalizedData)
        {
            try
            {
                var cronograma = new StringBuilder();

                cronograma.AppendLine("CRONOGRAMA DE CUOTAS");
                cronograma.AppendLine($"Total: ${montoTotal:N2} en {cuotas} cuotas");
                cronograma.AppendLine();

                bool usedRealData = false;

                // Intentar usar datos reales del escaneo
                for (int i = 0; i < cuotas; i++)
                {
                    var fechaKey = $"pago.cuotas[{i}].vencimiento";
                    var montoKey = $"pago.cuotas[{i}].prima";

                    if (normalizedData.ContainsKey(fechaKey) && normalizedData.ContainsKey(montoKey))
                    {
                        var fechaRaw = normalizedData[fechaKey].ToString();
                        var montoRaw = normalizedData[montoKey].ToString();

                        var fechaMatch = Regex.Match(fechaRaw, @"(\d{2}[-/]\d{2}[-/]\d{4})");
                        var fecha = fechaMatch.Success ? fechaMatch.Groups[1].Value : "Fecha no disponible";

                        var montoMatch = Regex.Match(montoRaw, @"([\d.,]+)");
                        var monto = montoMatch.Success ? montoMatch.Groups[1].Value : "0";

                        cronograma.AppendLine($"Cuota {i + 1:D2}: {fecha} - $ {monto}");
                        usedRealData = true;
                    }
                }

                // Si no hay datos reales, generar cronograma calculado
                if (!usedRealData)
                {
                    _logger.LogDebug("Generando cronograma calculado para {Cuotas} cuotas de ${MontoTotal}", cuotas, montoTotal);

                    var montoCuota = Math.Round((decimal)montoTotal / cuotas, 2);
                    var fechaBase = DateTime.Now; // Usar fecha base más apropiada según contexto

                    for (int i = 1; i <= cuotas; i++)
                    {
                        var fechaVencimiento = fechaBase.AddDays(30 * i);
                        var monto = i == cuotas ? montoTotal - (montoCuota * (cuotas - 1)) : montoCuota;
                        cronograma.AppendLine($"Cuota {i:D2}: {fechaVencimiento:dd/MM/yyyy} - ${monto:N2}");
                    }
                }

                cronograma.AppendLine("=== FIN CRONOGRAMA ===");

                return cronograma.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error generando cronograma de cuotas: {Error}", ex.Message);
                return "\nError generando cronograma de cuotas.";
            }
        }

        /// <summary>
        /// Genera observaciones automáticas basadas en los datos escaneados
        /// </summary>
        public List<string> GenerateAutomaticObservations(Dictionary<string, object> normalizedData)
        {
            var observations = new List<string>();

            // Detectar datos específicos que requieren atención
            if (normalizedData.ContainsKey("vehiculo.matricula"))
            {
                var matricula = normalizedData["vehiculo.matricula"].ToString();
                if (string.IsNullOrEmpty(matricula) || matricula == "PATENTE" || matricula == "MATRICULA")
                {
                    observations.Add("ATENCIÓN: Matrícula del vehículo no detectada correctamente");
                }
            }

            // Detectar inconsistencias en fechas
            if (normalizedData.ContainsKey("poliza.vigencia.desde") && normalizedData.ContainsKey("poliza.vigencia.hasta"))
            {
                var fechaDesde = normalizedData["poliza.vigencia.desde"].ToString();
                var fechaHasta = normalizedData["poliza.vigencia.hasta"].ToString();

                if (DateTime.TryParse(fechaDesde, out var desde) && DateTime.TryParse(fechaHasta, out var hasta))
                {
                    if (hasta <= desde)
                    {
                        observations.Add("ATENCIÓN: Fechas de vigencia inconsistentes - Revisar manualmente");
                    }

                    var duracion = (hasta - desde).Days;
                    if (duracion > 400) // Más de ~13 meses
                    {
                        observations.Add($"NOTA: Vigencia extendida detectada ({duracion} días) - Verificar si es correcto");
                    }
                }
            }

            // Detectar montos inusuales
            if (normalizedData.ContainsKey("financiero.premio_total"))
            {
                var premioStr = normalizedData["financiero.premio_total"].ToString();
                if (decimal.TryParse(premioStr.Replace("$", "").Replace(",", ""), out var premio))
                {
                    if (premio > 500000) // Más de $500k
                    {
                        observations.Add($"ATENCIÓN: Premio elevado (${premio:N2}) - Verificar monto");
                    }
                    else if (premio < 1000) // Menos de $1k
                    {
                        observations.Add($"ATENCIÓN: Premio bajo (${premio:N2}) - Verificar monto");
                    }
                }
            }

            return observations;
        }

        /// <summary>
        /// Combina observaciones automáticas con observaciones del usuario
        /// </summary>
        public string CombineObservations(
            List<string> automaticObservations,
            string? userNotes,
            string? userComments,
            string? additionalContext = null)
        {
            var allObservations = new List<string>();

            // Contexto adicional
            if (!string.IsNullOrEmpty(additionalContext))
            {
                allObservations.Add(additionalContext);
                allObservations.Add("");
            }

            // Observaciones automáticas
            if (automaticObservations.Any())
            {
                allObservations.Add("OBSERVACIONES AUTOMÁTICAS:");
                allObservations.AddRange(automaticObservations);
                allObservations.Add("");
            }

            // Notas del usuario
            if (!string.IsNullOrEmpty(userNotes))
            {
                allObservations.Add("NOTAS DEL USUARIO:");
                allObservations.Add(userNotes);
                allObservations.Add("");
            }

            // Comentarios del usuario
            if (!string.IsNullOrEmpty(userComments))
            {
                allObservations.Add("COMENTARIOS:");
                allObservations.Add(userComments);
            }

            return string.Join("\n", allObservations);
        }
    }
}