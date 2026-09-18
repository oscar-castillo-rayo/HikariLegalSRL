using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Models.Enums;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class ReporteService : IReporteService
    {
        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        // RF-012: tasa de conversión = propuestas Aceptadas ÷ total de propuestas Enviadas en el
        // período (Enviadas aquí es cualquier propuesta que salió de Borrador, sin importar si
        // ya fue resuelta). El período filtra por FechaEnvio, no por FechaCreacion, porque una
        // propuesta en Borrador (sin enviar) no participa todavía de ninguna tasa de conversión.
        public async Task<ReporteConversionPropuestasDTO> ObtenerConversionPropuestas(string periodo, DateTime? desde, DateTime? hasta)
        {
            var (desdeFinal, hastaFinal) = ResolverRango(periodo, desde, hasta);

            var estados = await _context.Propuestas
                .AsNoTracking()
                .Where(p => p.FechaEnvio != null
                    && p.FechaEnvio.Value.Date >= desdeFinal
                    && p.FechaEnvio.Value.Date <= hastaFinal)
                .Select(p => p.Estado)
                .ToListAsync();

            var enviadas = estados.Count(e => e == EstadoPropuesta.Enviada);
            var aceptadas = estados.Count(e => e == EstadoPropuesta.Aceptada);
            var rechazadas = estados.Count(e => e == EstadoPropuesta.Rechazada);
            var total = estados.Count;

            return new ReporteConversionPropuestasDTO
            {
                Periodo = periodo,
                Desde = desdeFinal,
                Hasta = hastaFinal,
                Total = total,
                Enviadas = enviadas,
                Aceptadas = aceptadas,
                Rechazadas = rechazadas,
                TasaConversion = total == 0 ? 0 : Math.Round((decimal)aceptadas / total * 100, 1)
            };
        }

        // Mismo criterio de zona horaria ya usado en RevisionVencimientosBackgroundService
        // (DateTime.UtcNow.Date) para no repetir el bug de +1 día ya documentado en otras
        // pantallas que usan DateTime.Today (hora local del servidor).
        private static (DateTime Desde, DateTime Hasta) ResolverRango(string periodo, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
                return (desde.Value.Date, hasta.Value.Date);

            var hoy = DateTime.UtcNow.Date;

            return periodo switch
            {
                "semanal" => (hoy.AddDays(-6), hoy),
                "cuatrimestral" => (hoy.AddMonths(-4), hoy),
                _ => (new DateTime(hoy.Year, hoy.Month, 1), hoy)
            };
        }
    }
}
