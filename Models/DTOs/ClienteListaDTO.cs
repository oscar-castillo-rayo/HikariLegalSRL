using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ClienteListaDTO
    {
        public int Id { get; set; }
        public string NombreEmpresaPersona { get; set; } = null!;
        public string? NombreContacto { get; set; }
        public string Correo { get; set; } = null!;
        public string? Telefono { get; set; }
        public ModalidadPago ModalidadPago { get; set; }
        public EstadoCliente Estado { get; set; }
        public string ResponsableNombre { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
    }
}
