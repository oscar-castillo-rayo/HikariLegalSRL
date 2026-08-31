using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IGeografiaService
    {
        Task<List<OpcionComboDTO>> ObtenerProvincias();
        Task<List<OpcionComboDTO>> ObtenerCantones(int provinciaId);
        Task<List<OpcionComboDTO>> ObtenerDistritos(int cantonId);
        Task<List<OpcionComboDTO>> ObtenerPaises();
    }
}
