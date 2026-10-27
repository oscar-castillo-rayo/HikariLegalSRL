using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define la consulta del catálogo geográfico: países, provincias, cantones y distritos.
    /// </summary>
    public interface IGeografiaService
    {
        Task<List<OpcionComboDTO>> ObtenerProvincias();
        Task<List<OpcionComboDTO>> ObtenerCantones(int provinciaId);
        Task<List<OpcionComboDTO>> ObtenerDistritos(int cantonId);
        Task<List<OpcionComboDTO>> ObtenerPaises();
    }
}
