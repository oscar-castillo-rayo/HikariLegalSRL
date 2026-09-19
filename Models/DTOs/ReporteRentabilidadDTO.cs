namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteRentabilidadDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int TotalExpedientes { get; set; }
        public int Rentables { get; set; }
        public int NoRentables { get; set; }
        public int SinHoras { get; set; }
        public int ProBono { get; set; }
        public decimal HorasProBono { get; set; }
        public decimal? EficienciaGlobal { get; set; }
        public List<RentabilidadTipoServicioDTO> PorTipoServicio { get; set; } = new();
        public List<RentabilidadExpedienteDTO> Expedientes { get; set; } = new();
    }
}
