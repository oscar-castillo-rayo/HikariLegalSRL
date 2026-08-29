namespace HikariLegalSRL.Services.Interfaces
{
    public interface ITransactionRunner
    {
        Task<TResultado> EjecutarAsync<TResultado>(Func<Task<TResultado>> operacion);
    }
}