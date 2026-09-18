using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.ViewModels.Facturas;

namespace HikariLegalSRL.Services.Interfaces
{
    public interface IFacturaService
    {
        Task<List<FacturaListaDTO>> Listar(string usuarioActualId);

        Task<FacturaDetalleDTO?> ObtenerDetalle(int facturaId, string usuarioActualId);

        Task<RegistrarAbonoViewModel?> ObtenerParaRegistrarAbono(int facturaId, string usuarioActualId);

        Task RegistrarAbono(int facturaId, AbonoRegistroDTO dto, string usuarioActualId);

        Task<(string RutaAbsoluta, string NombreArchivo, string ContentType)?> ObtenerArchivoComprobante(int abonoId, string usuarioActualId);
    }
}
