using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class RentabilidadTipoServicioDTO
    {
        public string AreaCategoria { get; set; } = null!;
        public Moneda Moneda { get; set; }
        public int Expedientes { get; set; }
        public decimal MontoFacturado { get; set; }
        public decimal HorasEstimadas { get; set; }
        public decimal HorasReales { get; set; }
        public decimal HorasColaborador { get; set; }
        public decimal HorasRevisor { get; set; }
        public decimal? ValorHoraEstimado { get; set; }
        public decimal? ValorHoraReal { get; set; }
        public decimal? Eficiencia { get; set; }
    }
}
