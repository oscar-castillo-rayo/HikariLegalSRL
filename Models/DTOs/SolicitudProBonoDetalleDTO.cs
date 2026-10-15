using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class SolicitudProBonoDetalleDTO
    {
        public int Id { get; set; }
        public int? ClienteId { get; set; }
        public int? ProspectoId { get; set; }
        public string Beneficiario { get; set; } = null!;
        public string SolicitanteId { get; set; } = null!;
        public string SolicitanteNombre { get; set; } = null!;
        public string JustificacionEscrita { get; set; } = null!;
        public DecisionProBono Decision { get; set; }
        public string? ComentarioResolucion { get; set; }
        public string? ResueltoPorNombre { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public int? PropuestaConsumidaId { get; set; }
    }
}
