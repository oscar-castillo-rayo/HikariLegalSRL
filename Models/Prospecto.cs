using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Prospecto
    {
        public int ProspectoId { get; set; }
        public string NombreEmpresaPersona { get; set; } = null!;
        public string NombreContacto { get; set; } = null!;
        public string? CedulaJuridica { get; set; }
        public string Telefono { get; set; } = null!;
        public string Correo { get; set; } = null!;

        public int DireccionId { get; set; }
        public Direccion Direccion { get; set; } = null!;

        public string? Sector { get; set; }
        public byte? Calificacion { get; set; }
        public string? Observaciones { get; set; }
        public EstadoProspecto Estado { get; set; } = EstadoProspecto.Activo;

        public string UsuarioCreadorId { get; set; } = null!;
        public ApplicationUser UsuarioCreador { get; set; } = null!;

        public DateTime FechaCreacion { get; set; }
    }
}