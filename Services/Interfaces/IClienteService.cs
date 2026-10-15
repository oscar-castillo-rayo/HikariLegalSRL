using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Clientes;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IClienteService
    {
        Task<int> ConvertirDesdeProspecto(int prospectoId, ClienteConversionDTO dto, string usuarioActualId);

        Task<List<ClienteListaDTO>> Listar(string? buscar);

        Task<ClienteDetalleDTO?> ObtenerDetalle(int id);

        Task<ClienteEditViewModel?> ObtenerParaEditar(int id);

        Task Editar(int id, ClienteEdicionDTO dto, string usuarioActualId);

        Task Desactivar(int id, string usuarioActualId);

        Task Reactivar(int id, string usuarioActualId);

        Task ReasignarResponsable(int id, string? nuevoResponsableId, string usuarioActualId);
    }
}
