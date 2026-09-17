using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Propuesta
    {
        public int PropuestaId { get; set; }

        public int? ProspectoId { get; set; }
        public Prospecto? Prospecto { get; set; }

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public int PlazoDias { get; set; }
        public ModalidadPago ModalidadPago { get; set; }
        public string? DescripcionGeneral { get; set; }
        public EstadoPropuesta Estado { get; set; } = EstadoPropuesta.Borrador;

        public string ElaboradaPorId { get; set; } = null!;
        public ApplicationUser ElaboradaPor { get; set; } = null!;

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaResolucion { get; set; }

        public List<PropuestaServicio> Servicios { get; set; } = new();
    }
}
