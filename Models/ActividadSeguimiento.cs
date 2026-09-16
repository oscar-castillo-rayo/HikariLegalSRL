using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class ActividadSeguimiento
    {
        public int ActividadSeguimientoId { get; set; }

        public int ProspectoId { get; set; }
        public Prospecto Prospecto { get; set; } = null!;

        public TipoActividad TipoActividad { get; set; }
        public string Titulo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateTime FechaHora { get; set; }

        public string UsuarioRegistroId { get; set; } = null!;
        public ApplicationUser UsuarioRegistro { get; set; } = null!;

        public string ResponsableId { get; set; } = null!;
        public ApplicationUser Responsable { get; set; } = null!;
    }
}
