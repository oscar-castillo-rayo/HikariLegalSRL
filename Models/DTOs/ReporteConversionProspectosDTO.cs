namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteConversionProspectosDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int Total { get; set; }
        public int Convertidos { get; set; }
        public int Descartados { get; set; }
        public int Activos { get; set; }
        public decimal TasaConversion { get; set; }
        public List<DetalleMensualProspectosDTO> DetalleMensual { get; set; } = new();
    }
}
