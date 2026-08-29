using HikariLegalSRL.Data;
using HikariLegalSRL.Exceptions;
using HikariLegalSRL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HikariLegalSRL.Services.Implementations
{
    public class TransactionRunner : ITransactionRunner
    {
        private readonly ApplicationDbContext _context;

        public TransactionRunner(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TResultado> EjecutarAsync<TResultado>(Func<Task<TResultado>> operacion)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultado = await operacion();
                await transaction.CommitAsync();
                return resultado;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                throw new ReglaNegocioException("No se pudo completar la operación. Verifique los datos ingresados.");
            }
        }
    }
}