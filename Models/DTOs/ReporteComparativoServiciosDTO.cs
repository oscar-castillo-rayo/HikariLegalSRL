namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteComparativoServiciosDTO
    {
        public string Periodo { get; set; } = null!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int ServiciosEnCatalogo { get; set; }
        public int ServiciosOfrecidos { get; set; }
        public int ServiciosSolicitados { get; set; }
        public int ServiciosSinDemanda { get; set; }
        public int PropuestasAceptadas { get; set; }
        public int UmbralDemandaAlta { get; set; }
        public int UmbralDemandaMedia { get; set; }
        public List<DemandaServicioDTO> Servicios { get; set; } = new();
    }
}
