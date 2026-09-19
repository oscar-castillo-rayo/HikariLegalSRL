using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.Models.DTOs
{
    public class CargaColaboradorDTO
    {
        public string ColaboradorId { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string? Especialidad { get; set; }
        public int Pendientes { get; set; }
        public int EnProceso { get; set; }
        public int Vencidas { get; set; }
        public int TotalActivas { get; set; }
        public NivelCarga Nivel { get; set; }
    }
}
