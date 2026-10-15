namespace HikariLegalSRL.Models.DTOs
{
    public class BitacoraPaginaDTO
    {
        public List<BitacoraRegistroDTO> Registros { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));
    }
}
