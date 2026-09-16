using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Servicios
{
    public class ServicioIndexViewModel
    {
        public string? Buscar { get; set; }
        public List<ServicioListaDTO> Servicios { get; set; } = new();
    }
}
