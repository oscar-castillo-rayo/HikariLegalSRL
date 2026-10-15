using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ClienteDetalleDTO
    {
        public int Id { get; set; }
        public int? ProspectoOrigenId { get; set; }
        public string NombreEmpresaPersona { get; set; } = null!;
        public string? NombreContacto { get; set; }
        public string? CedulaJuridica { get; set; }
        public string? Telefono { get; set; }
        public string Correo { get; set; } = null!;
        public string? SectorEconomico { get; set; }
        public ModalidadPago ModalidadPago { get; set; }
        public string ResponsableNombre { get; set; } = null!;
        public EstadoCliente Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        public string TipoUbicacion { get; set; } = null!;
        public string Pais { get; set; } = null!;
        public string? Provincia { get; set; }
        public string? Canton { get; set; }
        public string? Distrito { get; set; }
        public string? SenasExactas { get; set; }
    }
}
