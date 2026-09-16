using HikariLegalSRL.Models.DTOs;

namespace HikariLegalSRL.ViewModels.Servicios
{
    public class ServicioEditViewModel
    {
        public int Id { get; set; }

        public ServicioEdicionDTO Servicio { get; set; } = new();
    }
}
