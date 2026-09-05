using System.Globalization;
using Domiki.Web.Data;
using Domiki.Web.Infrastructure.Dto;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Применяет пачку намерений игрока в одной транзакции: каждая команда – под своей точкой сохранения.
/// </summary>
/// <remarks>
/// Отвергнутая команда не роняет пачку: транзакция откатывается к точке перед этой командой, а остальные применяются.
/// Откат к точке возвращает базу, но не <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker"/> – он
/// продолжает считать откатанные правки своими и записал бы их на коммите, поэтому отслеживание сбрасывается целиком.
/// Это безопасно: правки успевших команд уже отправлены в базу своим <c>SaveChanges</c>, следующие команды перечитают
/// состояние заново. Так же откатываются и отложенные действия – <see cref="UnitOfWork.AddAfterEventAction"/> от
/// неудавшейся команды не должен дожить до планировщика.
/// Кроме <see cref="BusinessException"/> так же отбивается <see cref="InvalidOperationException"/>: команда со ссылкой на
/// исчезнувший объект приходит из устаревшего снимка на экране игрока и не должна ронять применённых соседей –
/// <c>First</c>/<c>Single</c> в менеджерах на такой ссылке бросают именно её.
/// Один лок строки игрока на всю пачку: он берётся первой же командой и держится до конца транзакции, поэтому пачка
/// снижает конкуренцию, а не повышает её.
/// </remarks>
public class GameCommandBatch
{
    /// <summary>
    /// Потолок числа команд в одной пачке.
    /// </summary>
    /// <remarks>
    /// Граница доверия: пачка идёт одной транзакцией под локом строки игрока, и длина этой транзакции – цена для всех
    /// остальных его запросов. По замеру очередь кликов не превышает единиц команд, так что запас тридцатикратный.
    /// </remarks>
    public const int MaxCommands = 50;

    private readonly UnitOfWork _uow;
    private readonly ApplicationDbContext _context;
    private readonly GameCommandRegistry _registry;
    private readonly ILogger<GameCommandBatch> _logger;

    public GameCommandBatch(UnitOfWork uow, ApplicationDbContext context, GameCommandRegistry registry, ILogger<GameCommandBatch> logger)
    {
        _uow = uow;
        _context = context;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Применяет команды по порядку и возвращает итог каждой.
    /// </summary>
    /// <param name="playerId">Игрок, от чьего имени идут команды.</param>
    /// <param name="commands">Команды в порядке отправки.</param>
    /// <returns>Итоги в том же порядке.</returns>
    /// <exception cref="BusinessException">Команд больше <see cref="MaxCommands"/>.</exception>
    public GameCommandResultDto[] Apply(int playerId, GameCommandDto[] commands)
    {
        if (commands.Length > MaxCommands)
        {
            throw new BusinessException("Слишком много действий за раз, часть придётся повторить");
        }

        var results = new GameCommandResultDto[commands.Length];
        for (var index = 0; index < commands.Length; index++)
        {
            results[index] = ApplyOne(playerId, commands[index], index);
        }

        return results;
    }

    private GameCommandResultDto ApplyOne(int playerId, GameCommandDto command, int index)
    {
        if (command.CommandId == Guid.Empty)
        {
            throw new BusinessException("Действие пришло без опознавательного знака, повторите его");
        }

        if (PlayerCommandMarks.IsApplied(_context, playerId, command.CommandId))
        {
            return Result(command, GameCommandStatus.Duplicate, error: null);
        }

        var savepoint = string.Create(CultureInfo.InvariantCulture, $"command{index}");
        var afterEventActionCount = _uow.AfterEventActionCount;
        _uow.Transaction.CreateSavepoint(savepoint);
        try
        {
            if (!PlayerCommandMarks.TryMark(_uow, _context, playerId, command.CommandId))
            {
                _uow.Transaction.RollbackToSavepoint(savepoint);
                return Result(command, GameCommandStatus.Duplicate, error: null);
            }

            _registry.Execute(playerId, command.Kind, command.Args);
            _context.SaveChanges();
            _uow.Transaction.ReleaseSavepoint(savepoint);
            return Result(command, GameCommandStatus.Applied, error: null);
        }
        catch (BusinessException exception)
        {
            _logger.LogInformation("Бизнес-отказ {Kind}: {Message}", command.Kind, exception.Message);
            Undo(savepoint, afterEventActionCount);
            return Result(command, GameCommandStatus.Rejected, exception.Message);
        }
        catch (InvalidOperationException)
        {
            Undo(savepoint, afterEventActionCount);
            return Result(command, GameCommandStatus.Rejected, "Этого в деревне уже нет, обновите страницу");
        }
    }

    private void Undo(string savepoint, int afterEventActionCount)
    {
        _uow.Transaction.RollbackToSavepoint(savepoint);
        _context.ChangeTracker.Clear();
        _uow.DropAfterEventActionsAfter(afterEventActionCount);
    }

    private static GameCommandResultDto Result(GameCommandDto command, GameCommandStatus status, string? error) =>
        new() { CommandId = command.CommandId, Status = status.ToString(), Error = error };
}
