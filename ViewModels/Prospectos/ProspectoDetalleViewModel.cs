using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Prospectos
{
    public class ProspectoDetalleViewModel
    {
        public ProspectoDetalleDTO Prospecto { get; set; } = null!;
        public List<ActividadSeguimientoDTO> Actividades { get; set; } = new();
        public List<UsuarioOpcionDTO> UsuariosAsignables { get; set; } = new();
    }
}
