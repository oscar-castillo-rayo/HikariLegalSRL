using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Clientes
{
    public class ClienteIndexViewModel
    {
        public string? Buscar { get; set; }
        public List<ClienteListaDTO> Clientes { get; set; } = new();
    }
}
