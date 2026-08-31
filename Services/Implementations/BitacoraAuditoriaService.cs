using HikariLegalSRL.Controllers;
using HikariLegalSRL.Data;
using HikariLegalSRL.Services.Interfaces;

namespace HikariLegalSRL.Services.Implementations
{
    public class BitacoraAuditoriaService : IBitacoraAuditoriaService
    {
        private readonly ApplicationDbContext _context;

        public BitacoraAuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Crea una nueva instancia de BitacoraAuditoria con los valores proporcionados y la persiste en la base de datos
        public async Task Registrar(string usuarioId, string tipoAccion, string moduloAfectado, string registroAfectadoId, string? valorAnterior = null, string? valorNuevo = null)
        {
            var registro = new BitacoraAuditoria
            {
                UsuarioId = usuarioId,
                TipoAccion = tipoAccion,
                ModuloAfectado = moduloAfectado,
                RegistroAfectadoId = registroAfectadoId,
                ValorAnterior = valorAnterior,
                ValorNuevo = valorNuevo,
                FechaHora = DateTime.UtcNow
            };
            _context.BitacoraAuditoria.Add(registro); //Agrega el registro a la base de datos
        }
    }
}
