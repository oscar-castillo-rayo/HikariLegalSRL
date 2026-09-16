using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ServicioListaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string AreaCategoria { get; set; } = null!;
        public decimal PrecioBase { get; set; }
        public TipoServicio TipoServicio { get; set; }
        public EstadoServicio Estado { get; set; }
    }
}
