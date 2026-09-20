namespace HikariLegalSRL.Models.DTOs
{
    public class BitacoraFiltroDTO
    {
        public string? UsuarioId { get; set; }
        public string? TipoAccion { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
    }
}
