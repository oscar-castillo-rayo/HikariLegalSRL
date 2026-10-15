using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;

namespace HikariLegalSRL.ViewModels.Propuestas
{
    public class PropuestaIndexViewModel
    {
        public EstadoPropuesta? EstadoFiltro { get; set; }
        public List<PropuestaListaDTO> Propuestas { get; set; } = new();
    }
}
