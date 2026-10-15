using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IFacturaService
    {
        Task<List<FacturaListaDTO>> Listar(string usuarioActualId);

        Task<FacturaDetalleDTO?> ObtenerDetalle(int facturaId, string usuarioActualId);
    }
}
