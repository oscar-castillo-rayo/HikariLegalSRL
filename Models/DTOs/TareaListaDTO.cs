using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class TareaListaDTO
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public string ColaboradorId { get; set; } = null!;
        public string ColaboradorNombre { get; set; } = null!;
        public DateTime FechaLimite { get; set; }
        public decimal HorasEstimadas { get; set; }
        public PrioridadTarea Prioridad { get; set; }
        public EstadoTarea Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        public decimal HorasColaborador { get; set; }
        public decimal HorasRevisor { get; set; }
        public decimal HorasRealesTotal => HorasColaborador + HorasRevisor;

        public decimal HorasRondaActual { get; set; }
        public decimal HorasRevisionRondaActual { get; set; }

        public List<ArchivoDTO> ArchivosRondaActual { get; set; } = new();

        public List<RondaHistorialDTO> Historial { get; set; } = new();
    }
}
