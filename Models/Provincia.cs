namespace HikariLegalSRL.Models
{
    public class Provincia
    {
        public int ProvinciaId { get; set; }

        public int PaisId { get; set; }

        public string Nombre { get; set; }

        public Pais Pais { get; set; }
        public ICollection<Canton> Cantones { get; set; } = new HashSet<Canton>();

    }
}
