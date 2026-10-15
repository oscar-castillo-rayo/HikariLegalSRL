using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models
{
    public class Entregable
    {
        public int EntregableId { get; set; }

        public int TareaId { get; set; }
        public Tarea Tarea { get; set; } = null!;

        public string? ArchivoRuta { get; set; }

        public int RondaRevision { get; set; }
        public decimal HorasReales { get; set; }
        public TipoEntregable TipoEntregable { get; set; } = TipoEntregable.Preliminar;

        public string CargadoPorId { get; set; } = null!;
        public ApplicationUser CargadoPor { get; set; } = null!;

        public DateTime FechaCarga { get; set; }
    }
}
