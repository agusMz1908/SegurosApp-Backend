namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class VelneoPolizaRequest
    {
        public int clinro { get; set; }  // Cliente ID
        public int comcod { get; set; }  // Compañía ID  
        public int seccod { get; set; }  // Sección ID
        public string conpol { get; set; } = "";      // Número póliza
        public string conend { get; set; } = "0";     // Endoso
        public string confchdes { get; set; } = "";   // Fecha desde
        public string confchhas { get; set; } = "";   // Fecha hasta
        public int conpremio { get; set; } = 0;       // Premio
        public int contot { get; set; } = 0;          // Total
        public string conmaraut { get; set; } = "";   // Marca
        public string conmodaut { get; set; } = "";   // Modelo
        public int conanioaut { get; set; } = 0;      // Año
        public string conmotor { get; set; } = "";    // Motor
        public string conchasis { get; set; } = "";   // Chasis
        public int dptnom { get; set; } = 0;          // Departamento ID
        public string combustibles { get; set; } = "1"; // Combustible código
        public int desdsc { get; set; } = 0;          // Destino ID
        public int catdsc { get; set; } = 0;          // Categoría ID
        public int caldsc { get; set; } = 0;          // Calidad ID
        public int tarcod { get; set; } = 0;          // Tarifa ID
        public string consta { get; set; } = "1";     // Forma pago
        public int concuo { get; set; } = 1;          // Cuotas
        public string congesti { get; set; } = "1";   // Estado gestión
        public string contra { get; set; } = "1";     // Trámite
        public string convig { get; set; } = "1";     // Vigencia
        public int moncod { get; set; } = 858;        // Moneda (UYU)
        public DateTime ingresado { get; set; } = DateTime.UtcNow;
        public DateTime last_update { get; set; } = DateTime.UtcNow;
        public int app_id { get; set; } = 0;          // Referencia al scan
        public string? observaciones { get; set; }   // Notas adicionales
    }
}
