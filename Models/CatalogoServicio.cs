using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class CatalogoServicio
    {
        public int ServicioId { get; set; }

        public string Nombre { get; set; } = null!;
        public string AreaCategoria { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal PrecioBase { get; set; }
        public TipoServicio TipoServicio { get; set; }
        public EstadoServicio Estado { get; set; } = EstadoServicio.Activo;
    }
}
