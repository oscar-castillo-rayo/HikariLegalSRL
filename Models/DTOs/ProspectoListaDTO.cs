using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ProspectoListaDTO
    {
        public int Id { get; set; }
        public string NombreEmpresaPersona { get; set; } = null!;
        public string NombreContacto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Telefono { get; set; } = null!;
        public byte? Calificacion { get; set; }
        public EstadoProspecto Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
