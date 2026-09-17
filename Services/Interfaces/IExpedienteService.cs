using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Expedientes;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IExpedienteService
    {
        Task<List<ExpedienteListaDTO>> Listar();

        Task<ExpedienteDetalleViewModel?> ObtenerDetalle(int id);

        Task<List<UsuarioOpcionDTO>> ObtenerColaboradoresActivos();

        Task<List<UsuarioOpcionDTO>> ObtenerResponsablesActivos();

        Task AgregarTarea(int expedienteId, TareaCreacionDTO dto, string usuarioActualId);

        Task ReasignarResponsable(int expedienteId, string? nuevoResponsableId, string usuarioActualId);

        Task IniciarTarea(int tareaId, string usuarioActualId);

        Task MarcarListaParaRevision(int tareaId, CargarEntregableDTO dto, string usuarioActualId);

        Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoEntregable(int entregableId);
    }
}
