using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ExpedienteListaDTO
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
        public DateTime PlazoComprometido { get; set; }
        public EstadoExpediente Estado { get; set; }
        public int TotalTareas { get; set; }
        public int TareasAprobadas { get; set; }
    }
}
