using HikariLegalSRL.Models;

namespace HikariLegalSRL.Controllers
{
    public class BitacoraAuditoria
    {
        public long BitacoraAuditoriaId { get; set; }
        public string UsuarioId { get; set; } = null!;
        public ApplicationUser Usuario { get; set; } = null!;
        public string TipoAccion { get; set; } = null!;
        public string ModuloAfectado { get; set; } = null!;
        public string RegistroAfectadoId { get; set; } = null!;
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
        public DateTime FechaHora { get; set; }
    }
}
