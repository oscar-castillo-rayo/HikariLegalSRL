using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Expedientes;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IExpedienteService
    {
        Task<List<ExpedienteListaDTO>> Listar();

        Task<ExpedienteDetalleViewModel?> ObtenerDetalle(int id);

        Task<List<UsuarioOpcionDTO>> ObtenerColaboradoresActivos();

        Task AgregarTarea(int expedienteId, TareaCreacionDTO dto, string usuarioActualId);
    }
}
