namespace HikariLegalSRL.Models.DTOs
{
    public class ZonaGeograficaDTO
    {
        public string Nombre { get; set; } = null!;
        public string? Detalle { get; set; }
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
