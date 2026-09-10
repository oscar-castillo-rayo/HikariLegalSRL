using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ProspectoDetalleDTO
    {
        public int Id { get; set; }
        public string NombreEmpresaPersona { get; set; } = null!;
        public string NombreContacto { get; set; } = null!;
        public string? CedulaJuridica { get; set; }
        public string Telefono { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string? Sector { get; set; }
        public byte? Calificacion { get; set; }
        public string? Observaciones { get; set; }
        public EstadoProspecto Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string CreadoPor { get; set; } = null!;

        public string TipoUbicacion { get; set; } = null!;
        public string Pais { get; set; } = null!;
        public string? Provincia { get; set; }
        public string? Canton { get; set; }
        public string? Distrito { get; set; }
        public string? SenasExactas { get; set; }
    }
}
