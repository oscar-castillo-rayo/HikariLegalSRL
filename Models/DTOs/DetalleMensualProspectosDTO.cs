namespace HikariLegalSRL.Models.DTOs
{
    public class DetalleMensualProspectosDTO
    {
        public string NombreMes { get; set; } = null!;
        public int Registrados { get; set; }
        public int Convertidos { get; set; }
        public int Descartados { get; set; }
        public int Activos { get; set; }
        public decimal TasaConversion { get; set; }
    }
}
