using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IReporteService
    {
        Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteConversionProspectosDTO> ObtenerConversionProspectos(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteIngresosServicioDTO> ObtenerIngresosPorServicio(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteDistribucionGeograficaDTO> ObtenerDistribucionGeografica(string nivel);
    }
}
