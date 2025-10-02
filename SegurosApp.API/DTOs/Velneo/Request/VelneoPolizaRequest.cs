namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class VelneoPolizaRequest
    {
        // ✅ IDS PRINCIPALES
        public int clinro { get; set; }  // Cliente ID
        public int comcod { get; set; }  // Compañía ID  
        public int seccod { get; set; }  // Sección ID

        // ✅ DATOS DE PÓLIZA
        public string conpol { get; set; } = "";      // Número póliza
        public string conend { get; set; } = "0";     // Endoso
        public string confchdes { get; set; } = "";   // Fecha desde
        public string confchhas { get; set; } = "";   // Fecha hasta
        public int conpremio { get; set; } = 0;       // Premio
        public int contot { get; set; } = 0;          // Total
        public int? conpadre { get; set; }

        // ✅ DATOS DEL VEHÍCULO
        public string conmaraut { get; set; } = "";   // Marca
        public string conmodaut { get; set; } = "";   // Modelo
        public int conanioaut { get; set; } = 0;      // Año
        public string conmotor { get; set; } = "";    // Motor (se limpiará automáticamente)
        public string conchasis { get; set; } = "";   // Chasis (se limpiará automáticamente)
        public string conmataut { get; set; } = "";   // Matrícula
        public string conpadaut { get; set; } = "";

        // ✅ DATOS DEL CLIENTE - NUEVOS CAMPOS
        public string? clinom { get; set; }           // Nombre del cliente
        public string? condom { get; set; }           // Dirección del cliente
        public int clinro1 { get; set; } = 0;         // Cliente tomador (si es diferente)

        // ✅ MASTER DATA IDS
        public int dptnom { get; set; } = 0;          // Departamento ID
        public string combustibles { get; set; } = ""; // Combustible código
        public int desdsc { get; set; } = 0;          // Destino ID
        public int catdsc { get; set; } = 0;          // Categoría ID
        public int caldsc { get; set; } = 0;          // Calidad ID
        public int tarcod { get; set; } = 0;          // Tarifa ID
        public int corrnom { get; set; } = 0;         // Corredor ID

        // ✅ CONDICIONES DE PAGO
        public string consta { get; set; } = "";     // Forma pago (T=Tarjeta, 1=Contado, etc.)
        public int concuo { get; set; } = 1;          // Número de cuotas
        public int moncod { get; set; } = 0;        // Moneda (858=UYU, 840=USD)
        public int? conviamon { get; set; }

        // ✅ ESTADOS
        public string congesti { get; set; } = "1";   // Estado gestión
        public string? congeses { get; set; } = "";    // Estado gestión específico  
        public string contra { get; set; } = "1";     // Trámite
        public string convig { get; set; } = "1";     // Vigencia

        // ✅ DATOS ADICIONALES
        public string? com_alias { get; set; }        // Nombre de la compañía (desde comnom)
        public string? ramo { get; set; }             // Nombre de la sección (desde seccion)      // Estado gestión específico (texto que se mapea a número)

        // ✅ METADATOS
        public DateTime ingresado { get; set; } = DateTime.UtcNow;
        public DateTime last_update { get; set; } = DateTime.UtcNow;
        public int app_id { get; set; } = 0;          // Referencia al scan
        public string? observaciones { get; set; }    // Notas adicionales
    }
}
