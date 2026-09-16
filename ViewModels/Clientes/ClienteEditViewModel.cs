using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Clientes
{
    public class ClienteEditViewModel
    {
        public int Id { get; set; }

        public ClienteEdicionDTO Cliente { get; set; } = new() { Direccion = new() };

        public bool EsNacional { get; set; } = true;

        public int? ProvinciaIdActual { get; set; }
        public int? CantonIdActual { get; set; }

        public List<OpcionComboDTO> Provincias { get; set; } = new();
        public List<OpcionComboDTO> Paises { get; set; } = new();
    }
}
