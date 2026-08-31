namespace HikariLegalSRL.Models
{
    public class Canton
    {
        public int CantonId { get; set; }

        public int ProvinciaId { get; set; }

        public string Nombre { get; set; }

        public Provincia Provincia { get; set; }
        public ICollection<Distrito> Distritos { get; set; } = new HashSet<Distrito>();

    }
}
