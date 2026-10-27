using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define la obtención del resumen que muestra el dashboard de una persona usuaria.
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardDTO> ObtenerResumen(string usuarioId);
    }
}
