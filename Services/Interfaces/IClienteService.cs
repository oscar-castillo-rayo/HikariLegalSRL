using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IClienteService
    {
        Task<int> ConvertirDesdeProspecto(int prospectoId, ClienteConversionDTO dto, string usuarioActualId);

        Task<List<ClienteListaDTO>> Listar(string? buscar);

        Task<ClienteDetalleDTO?> ObtenerDetalle(int id);
    }
}
