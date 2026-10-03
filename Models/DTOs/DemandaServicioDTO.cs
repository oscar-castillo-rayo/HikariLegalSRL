using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class DemandaServicioDTO
    {
        public string Nombre { get; set; } = null!;
        public string AreaCategoria { get; set; } = null!;
        public TipoServicio Tipo { get; set; }
        public bool Activo { get; set; }
        public int VecesSolicitado { get; set; }
        public decimal Porcentaje { get; set; }
        public EstadoDemanda Demanda { get; set; }
    }
}
