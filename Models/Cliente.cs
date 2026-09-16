using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Cliente
    {
        public int ClienteId { get; set; }

        public int? ProspectoOrigenId { get; set; }
        public Prospecto? ProspectoOrigen { get; set; }

        public string NombreEmpresaPersona { get; set; } = null!;
        public string? NombreContacto { get; set; }
        public string? CedulaJuridica { get; set; }
        public string? Telefono { get; set; }
        public string Correo { get; set; } = null!;

        public int DireccionId { get; set; }
        public Direccion Direccion { get; set; } = null!;

        public string? SectorEconomico { get; set; }
        public ModalidadPago ModalidadPago { get; set; }

        public string ResponsableId { get; set; } = null!;
        public ApplicationUser Responsable { get; set; } = null!;

        public EstadoCliente Estado { get; set; } = EstadoCliente.Activo;

        public DateTime FechaCreacion { get; set; }
    }
}
