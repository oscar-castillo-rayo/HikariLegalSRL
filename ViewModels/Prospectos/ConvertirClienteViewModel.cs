using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Prospectos
{
    public class ConvertirClienteViewModel
    {
        public ProspectoDetalleDTO Prospecto { get; set; } = null!;
        public ClienteConversionDTO Cliente { get; set; } = new();
        public List<UsuarioOpcionDTO> Responsables { get; set; } = new();
    }
}
