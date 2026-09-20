using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Auditoria
{
    public class AuditoriaIndexViewModel
    {
        public BitacoraFiltroDTO Filtro { get; set; } = new();
        public BitacoraPaginaDTO Resultado { get; set; } = new();
        public List<UsuarioOpcionDTO> Usuarios { get; set; } = new();
    }
}
