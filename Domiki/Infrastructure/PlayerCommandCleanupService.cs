using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Фоновая чистка отметок применённых команд: удаляет записи старше <see cref="IdempotencyFilter.Retention"/>.
/// </summary>
/// <remarks>
/// Устроена так же, как <see cref="PlayerEventCleanupService"/>: приложение живёт в одном экземпляре, поэтому второго такого
/// воркера не бывает и распределённая блокировка не нужна; удаление идёт порциями, чтобы длинная транзакция не блокировала
/// вставку новых отметок.
/// </remarks>
public class PlayerCommandCleanupService : BackgroundService
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
    private readonly ILogger<PlayerCommandCleanupService> _logger;

    public PlayerCommandCleanupService(IServiceScopeFactory scopeFactory, ILogger<PlayerCommandCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Удаляет порцию отметок команд, применённых раньше указанного момента.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    /// <param name="cutoff">Момент, старше которого отметки удаляются.</param>
    /// <param name="batchSize">Потолок числа удаляемых строк за вызов.</param>
    /// <returns>Сколько строк удалено.</returns>
    /// <remarks>
    /// Порция отбирается подзапросом идентификаторов – тем же способом, что в <see cref="PlayerEventManager.DeleteOlderThan"/>.
    /// </remarks>
    public static int DeleteOlderThan(ApplicationDbContext context, DateTime cutoff, int batchSize)
    {
        var doomedIds = context.PlayerCommands
            .Where(x => x.AppliedDate < cutoff)
            .OrderBy(x => x.Id)
            .Take(batchSize)
            .Select(x => x.Id);

        return context.PlayerCommands.Where(x => doomedIds.Contains(x.Id)).ExecuteDelete();
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
                _logger.LogError(ex, "Чистка отметок команд не прошла");
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
        var cutoff = DateTimeHelper.GetNowDate() - IdempotencyFilter.Retention;
        var deleted = 0;

        for (var batch = 0; batch < MaxBatchesPerPass && !stoppingToken.IsCancellationRequested; batch++)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var removed = DeleteOlderThan(context, cutoff, BatchSize);
            deleted += removed;
            if (removed < BatchSize)
            {
                break;
            }
        }

        if (deleted > 0)
        {
            _logger.LogInformation("Чистка отметок команд: удалено {Deleted} записей старше {Cutoff:u}", deleted, cutoff);
        }
    }
}
