using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IProBonoService
    {
        Task<List<OpcionComboDTO>> ObtenerProspectosActivos();

        Task<List<OpcionComboDTO>> ObtenerClientesActivos();

        Task<List<SolicitudProBonoListaDTO>> Listar(string usuarioActualId);

        Task<SolicitudProBonoDetalleDTO?> ObtenerDetalle(int id, string usuarioActualId);

        Task<int> Crear(SolicitudProBonoCreacionDTO dto, string usuarioActualId);

        Task Resolver(int id, ResolverSolicitudProBonoDTO dto, string usuarioActualId);
    }
}
