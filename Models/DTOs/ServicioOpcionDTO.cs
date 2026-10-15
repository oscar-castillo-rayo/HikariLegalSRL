using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class ServicioOpcionDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal PrecioBase { get; set; }
        public TipoServicio TipoServicio { get; set; }
    }
}
