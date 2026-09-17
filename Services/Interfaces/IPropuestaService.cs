using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.ViewModels.Propuestas;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IPropuestaService
    {
        Task<List<OpcionComboDTO>> ObtenerProspectosActivos();

        Task<List<OpcionComboDTO>> ObtenerClientesActivos();

        Task<List<ServicioOpcionDTO>> ObtenerServiciosActivos();

        Task<List<PropuestaListaDTO>> Listar(string? buscar, EstadoPropuesta? estado);

        Task<PropuestaDetalleDTO?> ObtenerDetalle(int id);

        Task<int> Crear(PropuestaCreacionDTO dto, string usuarioActualId);

        Task<PropuestaEditViewModel?> ObtenerParaEditar(int id);

        Task Editar(int id, PropuestaEdicionDTO dto, string usuarioActualId);

        Task MarcarComoEnviada(int id, string usuarioActualId);

        Task<int> MarcarComoAceptada(int id, string usuarioActualId);

        Task MarcarComoRechazada(int id, string usuarioActualId);
    }
}
