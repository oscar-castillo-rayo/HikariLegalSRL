namespace HikariLegalSRL.Models.DTOs
{
    public class BitacoraRegistroDTO
    {
        public long Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string UsuarioNombre { get; set; } = null!;
        public string TipoAccion { get; set; } = null!;
        public string ModuloAfectado { get; set; } = null!;
        public string RegistroAfectadoId { get; set; } = null!;
        public string? EntidadNombre { get; set; }
        public string? ValorAnteriorResumen { get; set; }
        public bool ValorAnteriorTieneMas { get; set; }
        public string? ValorNuevoResumen { get; set; }
        public bool ValorNuevoTieneMas { get; set; }
    }
}
