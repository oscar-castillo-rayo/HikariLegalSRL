using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Expediente
    {
        public int ExpedienteId { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public int PropuestaId { get; set; }
        public Propuesta Propuesta { get; set; } = null!;

        public string ResponsableId { get; set; } = null!;
        public ApplicationUser Responsable { get; set; } = null!;

        public DateTime PlazoComprometido { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }

        public EstadoExpediente Estado { get; set; } = EstadoExpediente.Abierto;
    }
}
