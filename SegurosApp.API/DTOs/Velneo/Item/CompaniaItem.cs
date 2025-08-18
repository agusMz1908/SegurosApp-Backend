namespace SegurosApp.API.DTOs.Velneo.Item
{
    public class CompaniaItem
    {
        public int id { get; set; }
        public string comnom { get; set; } = string.Empty;         // Nombre completo
        public string comrazsoc { get; set; } = string.Empty;      // Razón social
        public string comruc { get; set; } = string.Empty;         // RUC
        public string comdom { get; set; } = string.Empty;         // Domicilio
        public string comtel { get; set; } = string.Empty;         // Teléfono
        public string comfax { get; set; } = string.Empty;         // Fax
        public string comsumodia { get; set; } = string.Empty;
        public int comcntcli { get; set; }                         // Cantidad clientes
        public int comcntcon { get; set; }                         // Cantidad contratos
        public int comprepes { get; set; }                         // Premio pesos
        public int compredol { get; set; }                         // Premio dólares
        public int comcomipe { get; set; }                         // Comisión pesos
        public int comcomido { get; set; }                         // Comisión dólares
        public int comtotcomi { get; set; }                        // Total comisión
        public int comtotpre { get; set; }                         // Total premio
        public string comalias { get; set; } = string.Empty;       // Alias/Código corto
        public string comlog { get; set; } = string.Empty;         // Logo
        public bool broker { get; set; }                           // Es broker
        public string cod_srvcompanias { get; set; } = string.Empty;
        public string no_utiles { get; set; } = string.Empty;      // HTML/notas
        public int paq_dias { get; set; }

        public string DisplayName => !string.IsNullOrEmpty(comnom) ? comnom : comalias;
        public bool IsActive => !string.IsNullOrEmpty(comnom); // Si tiene nombre, está activa
        public string ShortCode => comalias;
    }
}
