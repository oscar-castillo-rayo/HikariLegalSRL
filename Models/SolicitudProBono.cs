using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class SolicitudProBono
    {
        public int SolicitudProBonoId { get; set; }

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int? ProspectoId { get; set; }
        public Prospecto? Prospecto { get; set; }

        public string SolicitanteId { get; set; } = null!;
        public ApplicationUser Solicitante { get; set; } = null!;

        public string JustificacionEscrita { get; set; } = null!;

        public DecisionProBono Decision { get; set; } = DecisionProBono.Pendiente;
        public string? ComentarioResolucion { get; set; }

        public string? ResueltoPorId { get; set; }
        public ApplicationUser? ResueltoPor { get; set; }

        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaResolucion { get; set; }

        // Una solicitud aprobada solo puede respaldar una propuesta Pro Bono: se marca "consumida"
        // apenas esa propuesta se crea, para que no pueda reutilizarse en otra propuesta distinta.
        public int? PropuestaConsumidaId { get; set; }
        public Propuesta? PropuestaConsumida { get; set; }
    }
}
