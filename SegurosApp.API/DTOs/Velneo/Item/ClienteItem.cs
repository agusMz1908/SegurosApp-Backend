namespace SegurosApp.API.DTOs.Velneo.Item
{
    public class ClienteItem
    {
        public int id { get; set; }
        public int corrcod { get; set; }
        public int subcorr { get; set; }
        public string clinom { get; set; } = string.Empty;         // Nombre cliente
        public string telefono { get; set; } = string.Empty;
        public DateTime? clifchnac { get; set; }                   // Fecha nacimiento
        public DateTime? clifching { get; set; }                   // Fecha ingreso
        public DateTime? clifchegr { get; set; }                   // Fecha egreso
        public string clicargo { get; set; } = string.Empty;
        public string clicon { get; set; } = string.Empty;         // Contacto
        public string cliruc { get; set; } = string.Empty;         // RUC
        public string clirsoc { get; set; } = string.Empty;        // Razón social
        public string cliced { get; set; } = string.Empty;         // Cédula
        public string clilib { get; set; } = string.Empty;         // Libreta
        public string clicatlib { get; set; } = string.Empty;
        public string clitpo { get; set; } = string.Empty;         // Tipo
        public string clidir { get; set; } = string.Empty;         // Dirección
        public string cliemail { get; set; } = string.Empty;       // Email
        public DateTime? clivtoced { get; set; }                   // Vencimiento cédula
        public DateTime? clivtolib { get; set; }                   // Vencimiento libreta
        public int cliposcod { get; set; }
        public string clitelcorr { get; set; } = string.Empty;
        public string clidptnom { get; set; } = string.Empty;      // Departamento
        public string clisex { get; set; } = string.Empty;         // Sexo
        public string clitelant { get; set; } = string.Empty;
        public string cliobse { get; set; } = string.Empty;        // Observaciones
        public string clifax { get; set; } = string.Empty;
        public string clitelcel { get; set; } = string.Empty;      // Teléfono celular
        public string cliclasif { get; set; } = string.Empty;
        public string clinumrel { get; set; } = string.Empty;
        public string clicasapt { get; set; } = string.Empty;
        public string clidircob { get; set; } = string.Empty;
        public int clibse { get; set; }
        public string clifoto { get; set; } = string.Empty;
        public int pruebamillares { get; set; }
        public string ingresado { get; set; } = string.Empty;
        public string clialias { get; set; } = string.Empty;
        public string clipor { get; set; } = string.Empty;
        public string clisancor { get; set; } = string.Empty;
        public string clirsa { get; set; } = string.Empty;
        public int codposcob { get; set; }
        public string clidptcob { get; set; } = string.Empty;
        public bool activo { get; set; } = true;                   // Estado activo
        public string cli_s_cris { get; set; } = string.Empty;
        public DateTime? clifchnac1 { get; set; }
        public string clilocnom { get; set; } = string.Empty;      // Localidad
        public string cliloccob { get; set; } = string.Empty;
        public int categorias_de_cliente { get; set; }
        public string sc_departamentos { get; set; } = string.Empty;
        public string sc_localidades { get; set; } = string.Empty;
        public DateTime? fch_ingreso { get; set; }
        public int grupos_economicos { get; set; }
        public bool etiquetas { get; set; }
        public bool doc_digi { get; set; }
        public string password { get; set; } = string.Empty;
        public bool habilita_app { get; set; }
        public string referido { get; set; } = string.Empty;
        public int altura { get; set; }
        public int peso { get; set; }
        public string cliberkley { get; set; } = string.Empty;
        public string clifar { get; set; } = string.Empty;
        public string clisurco { get; set; } = string.Empty;
        public string clihdi { get; set; } = string.Empty;
        public string climapfre { get; set; } = string.Empty;
        public string climetlife { get; set; } = string.Empty;
        public string clisancris { get; set; } = string.Empty;
        public string clisbi { get; set; } = string.Empty;
        public string edo_civil { get; set; } = string.Empty;
        public bool not_bien_mail { get; set; }
        public bool not_bien_wap { get; set; }
        public bool ing_poliza_mail { get; set; }
        public bool ing_poliza_wap { get; set; }
        public bool ing_siniestro_mail { get; set; }
        public bool ing_siniestro_wap { get; set; }
        public bool noti_obs_sini_mail { get; set; }
        public bool noti_obs_sini_wap { get; set; }
        public DateTime? last_update { get; set; }
        public int app_id { get; set; }

        // 🎯 Propiedades calculadas para el frontend
        public string DisplayName => !string.IsNullOrEmpty(clirsoc) ? clirsoc : clinom;
        public string DocumentNumber => !string.IsNullOrEmpty(cliruc) ? cliruc : cliced;
        public string DocumentType => !string.IsNullOrEmpty(cliruc) ? "RUT" : "CI";
        public string FullAddress => $"{clidir}, {clilocnom}, {clidptnom}".Trim(' ', ',');
        public string ContactInfo => !string.IsNullOrEmpty(clitelcel) ? clitelcel : telefono;
    }
}
