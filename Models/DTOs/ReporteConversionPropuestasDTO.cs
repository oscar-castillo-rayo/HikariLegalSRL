namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteConversionPropuestasDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int Total { get; set; }
        public int Enviadas { get; set; }
        public int Aceptadas { get; set; }
        public int Rechazadas { get; set; }
        public decimal TasaConversion { get; set; }
    }
}
