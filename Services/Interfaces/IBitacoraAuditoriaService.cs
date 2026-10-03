using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IBitacoraAuditoriaService
    {
        Task Registrar(
            string usuarioId,
            string tipoAccion,
            string moduloAfectado,
            string registroAfectadoId,
            string? valorAnterior = null,
            string? valorNuevo = null);

        Task<BitacoraPaginaDTO> Consultar(BitacoraFiltroDTO filtro);

        Task<BitacoraDetalleDTO?> ObtenerDetalle(long id);

        Task<List<UsuarioOpcionDTO>> ObtenerUsuariosConRegistros();
    }
}