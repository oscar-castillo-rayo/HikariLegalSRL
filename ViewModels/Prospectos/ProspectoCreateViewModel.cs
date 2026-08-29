using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Prospectos
{
    public class ProspectoCreateViewModel
    {
        public ProspectoCreacionDTO Prospecto { get; set; } = new();

        public bool EsNacional { get; set; } = true;

        public List<OpcionComboDTO> Provincias { get; set; } = new();
        public List<OpcionComboDTO> Paises { get; set; } = new();
    }
}
