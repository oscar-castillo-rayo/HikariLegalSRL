namespace HikariLegalSRL.Models
{
    public class Direccion
    {
        public int DireccionId { get; set; }

        public int PaisId { get; set; }
        public Pais Pais { get; set; } = null!;

        public int? DistritoId { get; set; }
        public Distrito? Distrito { get; set; }

        public string? SenasExactas { get; set; }
        public string TipoUbicacion { get; set; } = "nacional";
        public DateTime FechaCreacion { get; set; }
    }
}