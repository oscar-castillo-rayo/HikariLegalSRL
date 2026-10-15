using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class RevisionEntregable
    {
        public int RevisionId { get; set; }

        public int EntregableId { get; set; }
        public Entregable Entregable { get; set; } = null!;

        public string RevisorId { get; set; } = null!;
        public ApplicationUser Revisor { get; set; } = null!;

        public ResultadoRevision Resultado { get; set; }
        public decimal HorasRevision { get; set; }
        public string? Observaciones { get; set; }
        public string? ArchivoAdjunto { get; set; }

        public DateTime FechaRevision { get; set; }
    }
}
