using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Expedientes;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IExpedienteService
    {
        Task<List<ExpedienteListaDTO>> Listar(string usuarioActualId);

        Task<ExpedienteDetalleViewModel?> ObtenerDetalle(int id, string usuarioActualId);

        Task<List<UsuarioOpcionDTO>> ObtenerColaboradoresActivos();

        Task<List<UsuarioOpcionDTO>> ObtenerResponsablesActivos();

        Task AgregarTarea(int expedienteId, TareaCreacionDTO dto, string usuarioActualId);

        Task EditarTarea(int tareaId, TareaEdicionDTO dto, string usuarioActualId);

        Task EliminarTarea(int tareaId, string usuarioActualId);

        Task ReasignarResponsable(int expedienteId, string? nuevoResponsableId, string usuarioActualId);

        Task IniciarTarea(int tareaId, string usuarioActualId);

        Task MarcarListaParaRevision(int tareaId, string usuarioActualId);

        Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoEntregable(int archivoId);

        Task AgregarArchivoEntregable(int tareaId, AgregarArchivoEntregableDTO dto, string usuarioActualId);

        Task EliminarArchivoEntregable(int archivoId, string usuarioActualId);

        Task AgregarHoras(int tareaId, AgregarHorasDTO dto, string usuarioActualId);

        Task AprobarEntregable(int entregableId, RevisarEntregableDTO dto, string usuarioActualId);

        Task DevolverEntregable(int entregableId, RevisarEntregableDTO dto, string usuarioActualId);

        Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoRevision(int revisionId);
    }
}
