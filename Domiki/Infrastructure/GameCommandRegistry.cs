using System.Text.Json;
using Domiki.Web.Core;
using Domiki.Web.Economy;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Реестр видов команд, которые клиент может прислать пачкой.
/// </summary>
/// <remarks>
/// Пять видов по замеру – 86 % всех действий игрока, и все пять про свой двор (см. <c>docs/offline-play.md</c>).
/// Реестр закрытый намеренно: команда, меняющая чужое состояние (ярмарка, помощь соседу, толока), пачкой не ходит.
/// Аргументы приходят объектом JSON, поэтому проверка полей – граница доверия: недостающее или чужого типа поле
/// заканчивается <see cref="BusinessException"/>, а не исключением разбора.
/// </remarks>
public class GameCommandRegistry
{
    private readonly Dictionary<string, Action<int, JsonElement>> _handlers;

    public GameCommandRegistry(DomikManager domikManager, OrderManager orderManager)
    {
        _handlers = new(StringComparer.Ordinal)
        {
            ["BuyDomik"] = (playerId, args) => domikManager.BuyDomik(playerId, GetInt(args, "typeId")),
            ["UpgradeDomik"] = (playerId, args) => domikManager.UpgradeDomik(playerId, GetInt(args, "domikId")),
            ["StartManufacture"] = (playerId, args) => domikManager.StartManufacture(
                playerId,
                GetInt(args, "domikId"),
                GetInt(args, "receiptId"),
                GetBool(args, "useOptional"),
                GetIntArray(args, "workerIds"),
                GetBool(args, "autoRepeat")),
            ["HurryManufacture"] = (playerId, args) => domikManager.HurryManufacture(playerId, GetInt(args, "manufactureId")),
            ["SetManufactureAutoRepeat"] = (playerId, args) => domikManager.SetManufactureAutoRepeat(playerId, GetInt(args, "manufactureId"), GetRequiredBool(args, "autoRepeat")),
            ["CompleteOrder"] = (playerId, args) => orderManager.CompleteOrder(playerId, GetInt(args, "orderId")),
        };
    }

    /// <summary>
    /// Виды команд, которые реестр умеет выполнять.
    /// </summary>
    /// <remarks>
    /// Список сверяется тестом с реестром клиента: разошедшиеся списки означают команду, которую клиент кладёт в очередь,
    /// а сервер отвергает.
    /// </remarks>
    public IEnumerable<string> Kinds => _handlers.Keys;

    /// <summary>
    /// Выполняет одну команду игрока.
    /// </summary>
    /// <param name="playerId">Игрок, от чьего имени идёт команда.</param>
    /// <param name="kind">Вид команды.</param>
    /// <param name="args">Аргументы команды.</param>
    /// <exception cref="BusinessException">Вид неизвестен или аргументы неполны.</exception>
    public void Execute(int playerId, string kind, JsonElement args)
    {
        if (!_handlers.TryGetValue(kind, out var handler))
        {
            throw new BusinessException("Такое дело деревня больше не понимает – обнови страницу");
        }

        handler(playerId, args);
    }

    private static JsonElement GetProperty(JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value))
        {
            throw new BusinessException("Действие пришло неполным, повторите его");
        }

        return value;
    }

    private static int GetInt(JsonElement args, string name)
    {
        var value = GetProperty(args, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            throw new BusinessException("Действие пришло неполным, повторите его");
        }

        return number;
    }

    private static bool GetRequiredBool(JsonElement args, string name)
    {
        var value = GetProperty(args, name);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new BusinessException("Действие пришло неполным, повторите его"),
        };
    }

    private static bool GetBool(JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new BusinessException("Действие пришло неполным, повторите его"),
        };
    }

    private static int[]? GetIntArray(JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new BusinessException("Действие пришло неполным, повторите его");
        }

        var items = new int[value.GetArrayLength()];
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var number))
            {
                throw new BusinessException("Действие пришло неполным, повторите его");
            }

            items[index++] = number;
        }

        return items;
    }
}
