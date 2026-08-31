using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IProspectoService
    {
        Task<int> Crear(ProspectoCreacionDTO dto, string usuarioActualId);
    }
}
