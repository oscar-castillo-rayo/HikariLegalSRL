using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class DemandaServicioDTO
    {
        public string Nombre { get; set; } = null!;
        public string AreaCategoria { get; set; } = null!;
        public TipoServicio Tipo { get; set; }
        public bool Activo { get; set; }
        public int VecesOfrecido { get; set; }
        public int VecesAceptado { get; set; }
        public decimal Participacion { get; set; }
        public decimal PorcentajeAceptacion { get; set; }
        public EstadoDemanda Demanda { get; set; }
    }
}
