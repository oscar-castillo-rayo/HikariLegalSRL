using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ExpedienteDetalleDTO
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public int PropuestaId { get; set; }
        public string ResponsableNombre { get; set; } = null!;
        public DateTime PlazoComprometido { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public EstadoExpediente Estado { get; set; }

        public List<TareaListaDTO> Tareas { get; set; } = new();
    }
}
