using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class RentabilidadExpedienteDTO
    {
        public int ExpedienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string AreaCategoria { get; set; } = null!;
        public Moneda Moneda { get; set; }
        public bool EsProBono { get; set; }
        public decimal MontoFacturado { get; set; }
        public decimal HorasEstimadas { get; set; }
        public decimal HorasReales { get; set; }
        public decimal HorasColaborador { get; set; }
        public decimal HorasRevisor { get; set; }
        public decimal? ValorHoraEstimado { get; set; }
        public decimal? ValorHoraReal { get; set; }
        public decimal? Diferencia { get; set; }
        public decimal? Eficiencia { get; set; }
        public bool? Rentable { get; set; }
    }
}
