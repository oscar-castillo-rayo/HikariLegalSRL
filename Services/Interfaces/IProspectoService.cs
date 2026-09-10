using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.ViewModels.Prospectos;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IProspectoService
    {
        Task<int> Crear(ProspectoCreacionDTO dto, string usuarioActualId);

        Task<List<ProspectoListaDTO>> Listar(string? buscar, byte? calificacion, EstadoProspecto? estado);

        Task<ProspectoDetalleDTO?> ObtenerDetalle(int id);

        Task<ProspectoEditViewModel?> ObtenerParaEditar(int id);

        Task Editar(int id, ProspectoEdicionDTO dto, string usuarioActualId);

        Task Descartar(int id, string usuarioActualId);

        Task Reactivar(int id, string usuarioActualId);
    }
}
