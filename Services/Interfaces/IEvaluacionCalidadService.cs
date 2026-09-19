using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IEvaluacionCalidadService
    {
        Task<List<EvaluacionCalidadListaDTO>> Listar();

        Task<EvaluacionCalidadDetalleDTO?> ObtenerDetalle(int evaluacionId);

        Task<ExpedienteEvaluableDTO?> ObtenerExpedienteEvaluable(int expedienteId);

        Task Registrar(int expedienteId, EvaluacionCalidadCreacionDTO dto, string usuarioActualId);
    }
}
