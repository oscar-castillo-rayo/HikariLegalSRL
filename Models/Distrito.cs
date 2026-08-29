namespace HikariLegalSRL.Models
{
    public class Distrito
    {
        public int DistritoId { get; set; }

        public int CantonId { get; set; }

        public string Nombre { get; set; }

        public Canton Canton { get; set; }

    }
}
