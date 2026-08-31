namespace HikariLegalSRL.Models
{
    public class Pais
    {
        public int PaisId { get; set; }

        public string Nombre { get; set; }

        public bool EsPaisBase { get; set; }

        public ICollection<Provincia> Provincias { get; set; } = new HashSet<Provincia>();

    }
}
