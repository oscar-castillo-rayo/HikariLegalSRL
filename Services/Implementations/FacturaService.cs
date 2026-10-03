using HikariLegalSRL.Constants;
using HikariLegalSRL.Data;
using HikariLegalSRL.Models.DTOs;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoEvaluador _permisoEvaluador;

        public FacturaService(ApplicationDbContext context, IPermisoEvaluador permisoEvaluador)
        {
            _context = context;
            _permisoEvaluador = permisoEvaluador;
        }

        // RF-010: el Abogado/Asesor solo consulta las facturas de su propia cartera.
        // Se usa Expediente.ResponsableId (no Cliente.ResponsableId) para que la visibilidad
        // siga a quien realmente gestiona ese caso puntual: si un expediente se reasigna
        // (HU-016) sin reasignar también al cliente, quien lo trabajó debe poder ver la
        // factura que generó, no el responsable original del cliente que ya no tiene relación
        // con ese caso. Administrador y Asistente (quienes pueden registrar abonos) ven todas.
        private async Task<bool> PuedeVerTodasAsync(string usuarioActualId)
        {
            var usuarioActual = await _context.Users.FindAsync(usuarioActualId);
            return usuarioActual is not null
                && await _permisoEvaluador.TienePermisoAsync(usuarioActual, Permisos.Facturacion.Abono);
        }

        public async Task<List<FacturaListaDTO>> Listar(string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var query = _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Expediente).ThenInclude(e => e.Propuesta)
                .AsQueryable();

            if (!puedeVerTodas)
                query = query.Where(f => f.Expediente.ResponsableId == usuarioActualId);

            return await query
                .OrderByDescending(f => f.FechaEmision)
                .Select(f => new FacturaListaDTO
                {
                    Id = f.FacturaId,
                    ExpedienteId = f.ExpedienteId,
                    ClienteNombre = f.Cliente.NombreEmpresaPersona,
                    ModalidadPago = f.ModalidadPago,
                    Moneda = f.Expediente.Propuesta.Moneda,
                    MontoTotal = f.MontoTotal,
                    Estado = f.Estado,
                    FechaEmision = f.FechaEmision
                })
                .ToListAsync();
        }

        public async Task<FacturaDetalleDTO?> ObtenerDetalle(int facturaId, string usuarioActualId)
        {
            var puedeVerTodas = await PuedeVerTodasAsync(usuarioActualId);

            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Expediente).ThenInclude(e => e.Responsable)
                .Include(f => f.Expediente).ThenInclude(e => e.Propuesta).ThenInclude(p => p.Servicios).ThenInclude(s => s.Servicio)
                .FirstOrDefaultAsync(f => f.FacturaId == facturaId);

            if (factura is null)
                return null;

            if (!puedeVerTodas && factura.Expediente.ResponsableId != usuarioActualId)
                return null;

            return new FacturaDetalleDTO
            {
                Id = factura.FacturaId,
                ExpedienteId = factura.ExpedienteId,
                ClienteNombre = factura.Cliente.NombreEmpresaPersona,
                ResponsableNombre = factura.Expediente.Responsable.NombreCompleto,
                ModalidadPago = factura.ModalidadPago,
                Moneda = factura.Expediente.Propuesta.Moneda,
                MontoTotal = factura.MontoTotal,
                Estado = factura.Estado,
                FechaEmision = factura.FechaEmision,
                FechaAnulacion = factura.FechaAnulacion,
                Servicios = factura.Expediente.Propuesta.Servicios
                    .Select(s => new FacturaServicioDTO
                    {
                        NombreServicio = s.Servicio.Nombre,
                        DescripcionServicio = s.DescripcionServicio,
                        Precio = s.Precio
                    })
                    .ToList()
            };
        }
    }
}
