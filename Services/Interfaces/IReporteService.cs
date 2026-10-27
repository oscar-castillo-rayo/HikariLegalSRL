using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define la obtención de los reportes de analítica de la firma.
    /// </summary>
    public interface IReporteService
    {
        Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteConversionProspectosDTO> ObtenerConversionProspectos(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteIngresosServicioDTO> ObtenerIngresosPorServicio(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteDistribucionGeograficaDTO> ObtenerDistribucionGeografica(string nivel);

        Task<ReporteCargaTrabajoDTO> ObtenerCargaTrabajo();

        Task<ReporteComparativoServiciosDTO> ObtenerComparativoServicios(string periodo, DateTime? desde, DateTime? hasta);

        Task<ReporteRentabilidadDTO> ObtenerRentabilidad(string periodo, DateTime? desde, DateTime? hasta);
    }
}
