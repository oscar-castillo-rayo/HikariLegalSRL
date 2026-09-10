using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ActividadSeguimientoDTO
    {
        public int Id { get; set; }
        public TipoActividad TipoActividad { get; set; }
        public string Titulo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateTime FechaHora { get; set; }
        public string RegistradoPor { get; set; } = null!;
        public string ResponsableId { get; set; } = null!;
        public string ResponsableNombre { get; set; } = null!;
    }
}
