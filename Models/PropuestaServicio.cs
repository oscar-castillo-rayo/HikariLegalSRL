using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class PropuestaServicio
    {
        public int PropuestaServicioId { get; set; }

        public int PropuestaId { get; set; }
        public Propuesta Propuesta { get; set; } = null!;

        public int ServicioId { get; set; }
        public CatalogoServicio Servicio { get; set; } = null!;

        public string? DescripcionServicio { get; set; }
        public decimal Precio { get; set; }
        public TipoServicio TipoServicio { get; set; }
    }
}
