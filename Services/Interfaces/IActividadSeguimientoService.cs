using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IActividadSeguimientoService
    {
        Task<List<ActividadSeguimientoDTO>> Listar(int prospectoId);

        Task<ActividadSeguimientoDTO?> Obtener(int actividadId);

        Task<int> Registrar(int prospectoId, ActividadSeguimientoFormDTO dto, string usuarioActualId);

        Task Editar(int actividadId, ActividadSeguimientoFormDTO dto, string usuarioActualId);

        Task Eliminar(int actividadId, string usuarioActualId);
    }
}
