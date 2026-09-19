namespace HikariLegalSRL.Models.DTOs
{
    public class ReporteCargaTrabajoDTO
    {
        public int UmbralCargaMedia { get; set; }
        public int UmbralCargaAlta { get; set; }
        public List<CargaColaboradorDTO> Colaboradores { get; set; } = new();
    }
}
