using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.ExceptionServices;

namespace HikariLegalSRL.Services.Implementations
{
    /// <summary>
    /// Implementa ITransaccionService: ejecuta operaciones en una transacción y las acciones posteriores a confirmarla o revertirla.
    /// </summary>
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

            var entidadesPrevias = CapturarEntidadesPrevias();
            var intento = 0;
            Exception? errorIntentoAnterior = null;
            ExceptionDispatchInfo? errorDeNegocio = null;
            T resultado;

            try
            {
                resultado = await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    intento++;
                    if (intento > 1)
                    {
                        _logger.LogWarning(errorIntentoAnterior, "Reintentando la transacción (intento {Intento}) tras un error transitorio de la base de datos.", intento);
                    }

                    await using var transaccion = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        var resultadoIntento = await operacion();
                        await _context.SaveChangesAsync();
                        await transaccion.CommitAsync();
                        return resultadoIntento;
                    }
                    catch (Exception ex)
                    {
                        errorIntentoAnterior = ex;
                        await RevertirAsync(transaccion);

                        try
                        {
                            RestaurarEntidadesPrevias(entidadesPrevias);
                        }
                        catch (Exception errorRestauracion)
                        {
                            _logger.LogError(errorRestauracion, "No se pudo restaurar el estado previo de las entidades tras un intento fallido.");
                        }

                        Ejecutar(_alRevertir, "revertir");
                        _alRevertir.Clear();
                        _alConfirmar.Clear();

                        if (ex is ReglaNegocioException or TransaccionFallidaException)
                        {
                            errorDeNegocio = ExceptionDispatchInfo.Capture(ex);
                            return default!;
                        }

                        throw;
                    }
                });
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }

            if (errorDeNegocio is not null)
            {
                _context.ChangeTracker.Clear();
                errorDeNegocio.Throw();
            }

            try
            {
                Ejecutar(_alConfirmar, "confirmar");
            }
            finally
            {
                _alConfirmar.Clear();
                _alRevertir.Clear();
            }

            return resultado;
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
        }

        private List<EntidadPrevia> CapturarEntidadesPrevias()
        {
            return _context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Unchanged or EntityState.Modified or EntityState.Deleted)
                .Select(e => new EntidadPrevia(e, e.State, e.OriginalValues.Clone(), e.CurrentValues.Clone()))
                .ToList();
        }

        private void RestaurarEntidadesPrevias(List<EntidadPrevia> entidadesPrevias)
        {
            var previas = entidadesPrevias.Select(p => p.Entrada.Entity).ToHashSet(ReferenceEqualityComparer.Instance);

            foreach (var entrada in _context.ChangeTracker.Entries().ToList())
            {
                if (!previas.Contains(entrada.Entity))
                    entrada.State = EntityState.Detached;
            }

            foreach (var previa in entidadesPrevias)
            {
                previa.Entrada.State = EntityState.Unchanged;
                previa.Entrada.OriginalValues.SetValues(previa.Originales);
                previa.Entrada.CurrentValues.SetValues(previa.Actuales);

                if (previa.Estado == EntityState.Deleted)
                    previa.Entrada.State = EntityState.Deleted;
            }
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

        private sealed record EntidadPrevia(EntityEntry Entrada, EntityState Estado, PropertyValues Originales, PropertyValues Actuales);
    }
}
