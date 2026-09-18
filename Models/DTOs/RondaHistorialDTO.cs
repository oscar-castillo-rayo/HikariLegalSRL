using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class RondaHistorialDTO
    {
        public int RondaRevision { get; set; }
        public int EntregableId { get; set; }
        public List<ArchivoDTO> Archivos { get; set; } = new();
        public decimal HorasReales { get; set; }
        public DateTime FechaCarga { get; set; }

        public int? RevisionId { get; set; }
        public ResultadoRevision? Resultado { get; set; }
        public string? RevisorNombre { get; set; }
        public decimal? HorasRevision { get; set; }
        public string? Observaciones { get; set; }
        public bool TieneArchivoRevision { get; set; }
        public DateTime? FechaRevision { get; set; }
    }
}
