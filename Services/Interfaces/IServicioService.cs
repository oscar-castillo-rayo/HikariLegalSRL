using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Servicios;

namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define las operaciones del catálogo de servicios: listar, crear, editar, desactivar y reactivar.
    /// </summary>
    public interface IServicioService
    {
        Task<List<ServicioListaDTO>> Listar(string? buscar);

        Task<ServicioEditViewModel?> ObtenerParaEditar(int id);

        Task<int> Crear(ServicioCreacionDTO dto, string usuarioActualId);

        Task Editar(int id, ServicioEdicionDTO dto, string usuarioActualId);

        Task Desactivar(int id, string usuarioActualId);

        Task Reactivar(int id, string usuarioActualId);
    }
}
