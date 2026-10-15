using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Propuestas
{
    public class PropuestaEditViewModel
    {
        public int Id { get; set; }

        public PropuestaEdicionDTO Propuesta { get; set; } = new();

        public List<OpcionComboDTO> Prospectos { get; set; } = new();
        public List<OpcionComboDTO> Clientes { get; set; } = new();
        public List<ServicioOpcionDTO> Servicios { get; set; } = new();
        public BeneficiariosProBonoDTO BeneficiariosProBono { get; set; } = new();
    }
}
