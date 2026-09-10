using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Prospectos
{
    public class ProspectoIndexViewModel
    {
        public List<ProspectoListaDTO> Prospectos { get; set; } = new();

        public string? Buscar { get; set; }
        public byte? Calificacion { get; set; }
        public int? Estado { get; set; }
    }
}
