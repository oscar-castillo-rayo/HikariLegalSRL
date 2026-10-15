using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class PropuestaDetalleDTO
    {
        public int Id { get; set; }
        public string Destinatario { get; set; } = null!;
        public bool EsProspecto { get; set; }
        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public int PlazoDias { get; set; }
        public ModalidadPago ModalidadPago { get; set; }
        public string? DescripcionGeneral { get; set; }
        public EstadoPropuesta Estado { get; set; }
        public string ElaboradaPorNombre { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaResolucion { get; set; }

        public List<PropuestaServicioDetalleDTO> Servicios { get; set; } = new();
    }

    public class PropuestaServicioDetalleDTO
    {
        public string ServicioNombre { get; set; } = null!;
        public string? DescripcionServicio { get; set; }
        public string? DescripcionCatalogo { get; set; }
        public decimal Precio { get; set; }
        public TipoServicio TipoServicio { get; set; }
    }
}
