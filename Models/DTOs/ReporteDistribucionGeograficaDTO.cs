namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteDistribucionGeograficaDTO
    {
        public string Nivel { get; set; } = null!;
        public int TotalClientes { get; set; }
        public List<ZonaGeograficaDTO> Zonas { get; set; } = new();
    }
}
