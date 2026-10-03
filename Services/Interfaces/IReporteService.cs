using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IReporteService
    {
        Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta);
    }
}
