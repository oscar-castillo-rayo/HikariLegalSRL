using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class SolicitudProBonoListaDTO
    {
        public int Id { get; set; }
        public string Beneficiario { get; set; } = null!;
        public string SolicitanteNombre { get; set; } = null!;
        public string JustificacionEscrita { get; set; } = null!;
        public DecisionProBono Decision { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string? ResueltoPorNombre { get; set; }
        public DateTime? FechaResolucion { get; set; }
    }
}
