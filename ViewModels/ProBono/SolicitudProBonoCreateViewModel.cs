using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.ProBono
{
    public class SolicitudProBonoCreateViewModel
    {
        public SolicitudProBonoCreacionDTO Solicitud { get; set; } = new();

        public List<OpcionComboDTO> Prospectos { get; set; } = new();
        public List<OpcionComboDTO> Clientes { get; set; } = new();
    }
}
