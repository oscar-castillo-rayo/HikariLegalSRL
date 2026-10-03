namespace HikariLegalSRL.Services.Interfaces
{
    public interface ITransaccionService
    {
        Task EjecutarAsync(Func<Task> operacion);
        Task<T> EjecutarAsync<T>(Func<Task<T>> operacion);
        void AlConfirmar(Action accion);
        void AlRevertir(Action accion);
    }
}
