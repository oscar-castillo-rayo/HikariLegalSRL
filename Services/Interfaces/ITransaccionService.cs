namespace HikariLegalSRL.Services.Interfaces
{
    /// <summary>
    /// Define la ejecución de operaciones dentro de una transacción de base de datos, con acciones que se ejecutan al confirmar o al revertir.
    /// </summary>
    public interface ITransaccionService
    {
        Task EjecutarAsync(Func<Task> operacion);
        Task<T> EjecutarAsync<T>(Func<Task<T>> operacion);
        void AlConfirmar(Action accion);
        void AlRevertir(Action accion);
    }
}
