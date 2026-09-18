using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDTO> ObtenerResumen(string usuarioId);
    }
}
