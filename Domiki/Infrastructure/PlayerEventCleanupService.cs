namespace Domiki.Web.Infrastructure;

/// <summary>
/// Фоновая чистка журнала событий: удаляет записи старше окна хранения <see cref="PlayerEventManager.Retention"/>.
/// </summary>
/// <remarks>
/// Вынесена из <see cref="PlayerEventManager.TakeRecap"/>, который прежде удалял строки прямо в транзакции запроса
/// <c>GetGameState</c> и держал журнал на потолке в 50 записей. Приложение живёт в одном экземпляре (in-memory планировщик
/// <see cref="Core.Scheduling.Calculator"/>), поэтому второго такого воркера в системе не бывает и распределённая блокировка не нужна.
/// Работает порциями: длинная транзакция на всей таблице блокировала бы записи новых событий.
/// </remarks>
public class PlayerEventCleanupService : BackgroundService
{
    /// <summary>
    /// Пауза между проходами чистки.
    /// </summary>
    private static readonly TimeSpan Period = TimeSpan.FromHours(6);

    /// <summary>
    /// Сколько строк удаляется за одну порцию.
    /// </summary>
    private const int BatchSize = 500;

    /// <summary>
    /// Потолок порций за один проход – защита от бесконечного цикла, если удаление не сокращает выборку.
    /// </summary>
    private const int MaxBatchesPerPass = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlayerEventCleanupService> _logger;

    public PlayerEventCleanupService(IServiceScopeFactory scopeFactory, ILogger<PlayerEventCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Cleanup(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Чистка журнала событий не прошла");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void Cleanup(CancellationToken stoppingToken)
    {
        var cutoff = DateTimeHelper.GetNowDate() - PlayerEventManager.Retention;
        var deleted = 0;

        for (var batch = 0; batch < MaxBatchesPerPass && !stoppingToken.IsCancellationRequested; batch++)
        {
            using var scope = _scopeFactory.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<PlayerEventManager>();
            var removed = manager.DeleteOlderThan(cutoff, BatchSize);
            deleted += removed;
            if (removed < BatchSize)
            {
                break;
            }
        }

        if (deleted > 0)
        {
            _logger.LogInformation("Чистка журнала: удалено {Deleted} событий старше {Cutoff:u}", deleted, cutoff);
        }
    }
}
