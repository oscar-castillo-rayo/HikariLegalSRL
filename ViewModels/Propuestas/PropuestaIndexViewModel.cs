using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.ViewModels.Propuestas
{
    public class PropuestaIndexViewModel
    {
        public string? Buscar { get; set; }
        public EstadoPropuesta? EstadoFiltro { get; set; }
        public List<PropuestaListaDTO> Propuestas { get; set; } = new();
    }
}
