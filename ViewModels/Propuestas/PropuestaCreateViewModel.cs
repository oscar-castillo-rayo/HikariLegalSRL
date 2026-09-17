using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Propuestas
{
    public class PropuestaCreateViewModel
    {
        public PropuestaCreacionDTO Propuesta { get; set; } = new();

        public List<OpcionComboDTO> Prospectos { get; set; } = new();
        public List<OpcionComboDTO> Clientes { get; set; } = new();
        public List<ServicioOpcionDTO> Servicios { get; set; } = new();
    }
}
