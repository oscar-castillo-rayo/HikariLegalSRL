using HikariLegalSRL.Data;

namespace HikariLegalSRL.Services.Implementations
{
    public class DbSeedBackgroundService : BackgroundService
    {
        private const int MaxIntentos = 3;
        private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<DbSeedBackgroundService> _logger;

        public DbSeedBackgroundService(IServiceScopeFactory scopeFactory, IHostApplicationLifetime lifetime, ILogger<DbSeedBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _lifetime = lifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await EsperarArranqueAsync(stoppingToken);

                for (var intento = 1; intento <= MaxIntentos; intento++)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        await DbInitializer.SeedAsync(scope.ServiceProvider);
                        _logger.LogInformation("Seed de la base de datos completado.");
                        return;
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "El seed de la base de datos falló (intento {Intento} de {MaxIntentos}).", intento, MaxIntentos);
                    }

                    if (intento < MaxIntentos)
                        await Task.Delay(EsperaEntreIntentos, stoppingToken);
                }

                _logger.LogError("El seed de la base de datos no se completó tras {MaxIntentos} intentos. Se reintentará en el próximo arranque.", MaxIntentos);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task EsperarArranqueAsync(CancellationToken stoppingToken)
        {
            var arrancada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var registroArranque = _lifetime.ApplicationStarted.Register(() => arrancada.TrySetResult());
            using var registroCancelacion = stoppingToken.Register(() => arrancada.TrySetCanceled(stoppingToken));

            await arrancada.Task;
        }
    }
}
