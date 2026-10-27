using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define las operaciones de las evaluaciones de calidad: listar, consultar el detalle, obtener el expediente evaluable y registrar la evaluación.
    /// </summary>
    public interface IEvaluacionCalidadService
    {
        Task<List<EvaluacionCalidadListaDTO>> Listar();

        Task<EvaluacionCalidadDetalleDTO?> ObtenerDetalle(int evaluacionId);

        Task<ExpedienteEvaluableDTO?> ObtenerExpedienteEvaluable(int expedienteId);

        Task Registrar(int expedienteId, EvaluacionCalidadCreacionDTO dto, string usuarioActualId);
    }
}
