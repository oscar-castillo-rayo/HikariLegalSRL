namespace HikariLegalSRL.Services.Interfaces
{
    public interface IBitacoraAuditoriaService
    {
        Task Registrar(
            string usuarioId,
            string tipoAccion,
            string moduloAfectado,
            string registroAfectadoId,
            string? valorAnterior = null,
            string? valorNuevo = null);
    }
}