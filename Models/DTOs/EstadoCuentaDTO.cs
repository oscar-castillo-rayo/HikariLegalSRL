namespace HikariLegalSRL.Models.DTOs
{
    public class EstadoCuentaDTO
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public List<TotalPorMonedaDTO> Totales { get; set; } = new();
        public List<FacturaListaDTO> Facturas { get; set; } = new();
    }
}
