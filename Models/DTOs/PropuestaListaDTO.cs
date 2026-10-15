using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class PropuestaListaDTO
    {
        public int Id { get; set; }
        public string Destinatario { get; set; } = null!;
        public bool EsProspecto { get; set; }
        public Moneda Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public EstadoPropuesta Estado { get; set; }
        public ModalidadPago ModalidadPago { get; set; }
        public int PlazoDias { get; set; }
        public string ElaboradaPorNombre { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
    }
}
