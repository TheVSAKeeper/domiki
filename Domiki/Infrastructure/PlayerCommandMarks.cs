using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Отметки применённых команд: проверка повтора и постановка отметки под точкой сохранения.
/// </summary>
/// <remarks>
/// Общая часть <see cref="IdempotencyFilter"/> (одна команда в запросе) и <see cref="GameCommandBatch"/> (пачка): у обоих
/// повтор должен опознаваться одинаково, иначе одна и та же команда, доехавшая по разным путям, применится дважды.
/// </remarks>
public static class PlayerCommandMarks
{
    private const string SavepointName = "idempotency";

    private const string UniqueViolation = "23505";

    /// <summary>
    /// Проверяет, применялась ли команда раньше.
    /// </summary>
    /// <param name="dbContext">Контекст базы данных.</param>
    /// <param name="playerId">Игрок.</param>
    /// <param name="commandId">Идентификатор команды.</param>
    /// <returns><c>true</c>, если отметка уже стоит.</returns>
    public static bool IsApplied(ApplicationDbContext dbContext, int playerId, Guid commandId) =>
        dbContext.PlayerCommands.Any(x => x.PlayerId == playerId && x.CommandId == commandId);

    /// <summary>
    /// Ставит отметку о команде, если её ещё нет.
    /// </summary>
    /// <param name="uow">Единица работы – её транзакция держит точку сохранения.</param>
    /// <param name="dbContext">Контекст базы данных.</param>
    /// <param name="playerId">Игрок.</param>
    /// <param name="commandId">Идентификатор команды.</param>
    /// <returns><c>false</c>, если отметку уже поставила параллельная отправка.</returns>
    /// <remarks>
    /// Гонка ловится уникальным индексом под точкой сохранения: без неё ошибка уникальности перевела бы транзакцию Postgres
    /// в состояние aborted и уронила бы коммит запроса.
    /// </remarks>
    public static bool TryMark(UnitOfWork uow, ApplicationDbContext dbContext, int playerId, Guid commandId)
    {
        var mark = new Data.Entities.PlayerCommand
        {
            PlayerId = playerId,
            CommandId = commandId,
            AppliedDate = DateTimeHelper.GetNowDate(),
        };

        dbContext.PlayerCommands.Add(mark);
        uow.Transaction.CreateSavepoint(SavepointName);
        try
        {
            dbContext.SaveChanges();
            uow.Transaction.ReleaseSavepoint(SavepointName);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            uow.Transaction.RollbackToSavepoint(SavepointName);
            dbContext.Entry(mark).State = EntityState.Detached;
            return false;
        }
    }
}
