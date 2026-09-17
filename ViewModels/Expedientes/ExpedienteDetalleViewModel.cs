using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Expedientes
{
    public class ExpedienteDetalleViewModel
    {
        public ExpedienteDetalleDTO Expediente { get; set; } = null!;
        public TareaCreacionDTO NuevaTarea { get; set; } = new();
        public List<UsuarioOpcionDTO> Colaboradores { get; set; } = new();
    }
}
