namespace HikariLegalSRL.Models.DTOs
{
    public class BitacoraDetalleDTO
    {
        public long Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string UsuarioNombre { get; set; } = null!;
        public string? UsuarioCorreo { get; set; }
        public string TipoAccion { get; set; } = null!;
        public string ModuloAfectado { get; set; } = null!;
        public string RegistroAfectadoId { get; set; } = null!;
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
    }
}
