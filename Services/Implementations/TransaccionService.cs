using HikariLegalSRL.Data;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HikariLegalSRL.Services.Implementations
{
    public class TransaccionService : ITransaccionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransaccionService> _logger;
        private readonly List<Action> _alConfirmar = new();
        private readonly List<Action> _alRevertir = new();

        public TransaccionService(ApplicationDbContext context, ILogger<TransaccionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task EjecutarAsync(Func<Task> operacion)
        {
            await EjecutarAsync<object?>(async () =>
            {
                await operacion();
                return null;
            });
        }

        public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion)
        {
            if (_context.Database.CurrentTransaction is not null)
                return await operacion();

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                var resultado = await operacion();
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                Ejecutar(_alConfirmar, "confirmar");
                return resultado;
            }
            catch
            {
                await RevertirAsync(transaccion);
                Ejecutar(_alRevertir, "revertir");
                throw;
            }
            finally
            {
                _alConfirmar.Clear();
                _alRevertir.Clear();
            }
        }

        public void AlConfirmar(Action accion)
        {
            ExigirTransaccion();
            _alConfirmar.Add(accion);
        }

        public void AlRevertir(Action accion)
        {
            ExigirTransaccion();
            _alRevertir.Add(accion);
        }

        private void ExigirTransaccion()
        {
            if (_context.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Solo se puede registrar una acción diferida dentro de EjecutarAsync.");
        }

        private async Task RevertirAsync(IDbContextTransaction transaccion)
        {
            try
            {
                await transaccion.RollbackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo revertir la transacción.");
            }

            _context.ChangeTracker.Clear();
        }

        private void Ejecutar(List<Action> acciones, string momento)
        {
            foreach (var accion in acciones)
            {
                try
                {
                    accion();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falló una acción diferida al {Momento} la transacción.", momento);
                }
            }
        }
    }
}
