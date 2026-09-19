namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteIngresosServicioDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<IngresosPorMonedaDTO> Monedas { get; set; } = new();
    }
}
