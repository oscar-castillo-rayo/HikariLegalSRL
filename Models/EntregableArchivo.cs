namespace HikariLegalSRL.Models
{
    public class EntregableArchivo
    {
        public int EntregableArchivoId { get; set; }

        public int EntregableId { get; set; }
        public Entregable Entregable { get; set; } = null!;

        public string ArchivoRuta { get; set; } = null!;
        public string NombreOriginal { get; set; } = null!;

        public string CargadoPorId { get; set; } = null!;
        public ApplicationUser CargadoPor { get; set; } = null!;

        public DateTime FechaCarga { get; set; }
    }
}
