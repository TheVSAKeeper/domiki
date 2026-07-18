namespace Domiki.Web.Activities.Models;

/// <summary>
/// Активная (или только что завершённая) толока.
/// </summary>
/// <remarks>
/// Общесерверный проект, наполняемый вкладами всех игроков; на клиент отдаётся как <see cref="Dto.TolokaDto"/>.
/// </remarks>
public class Toloka
{
    /// <summary>
    /// Идентификатор инстанции толоки.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип толоки.
    /// </summary>
    public required TolokaType TolokaType { get; set; }

    /// <summary>
    /// Позиции корзины сбора этой инстанции – цель, собрано и вклад самого игрока по каждому ресурсу.
    /// </summary>
    public TolokaPosition[] Positions { get; set; } = [];

    /// <summary>
    /// Момент начала текущей толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }
}

/// <summary>
/// Одна позиция корзины сбора инстанции толоки.
/// </summary>
public class TolokaPosition
{
    /// <summary>
    /// Ресурс позиции, – ссылка на <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Целевое количество ресурса, при достижении которого позиция считается набранной.
    /// </summary>
    public int Goal { get; set; }

    /// <summary>
    /// Сколько ресурса уже внесено всеми игроками по этой позиции.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Goal"/>.
    /// </remarks>
    public int Collected { get; set; }

    /// <summary>
    /// Сколько ресурса внёс сам игрок в эту позицию.
    /// </summary>
    /// <remarks>
    /// Входит в общий <see cref="Collected"/>.
    /// </remarks>
    public int MyContribution { get; set; }
}

/// <summary>
/// Состояние толоки для конкретного игрока.
/// </summary>
/// <remarks>
/// Собирается в <see cref="TolokaManager.GetToloka"/> и отдаётся на клиент как <see cref="Dto.TolokaStateDto"/>.
/// </remarks>
public class TolokaState
{
    /// <summary>
    /// Текущая активная толока.
    /// </summary>
    public required Toloka Active { get; set; }

    /// <summary>
    /// Бонусы производству от недавно завершённых толок, всё ещё действующие игроку.
    /// </summary>
    /// <remarks>
    /// Пустой массив – бонусов нет.
    /// </remarks>
    public TolokaActiveBuff[] ActiveBuffs { get; set; } = [];

    /// <summary>
    /// Продолжительность бонуса от завершения толоки – зависит от уровня «Сходни».
    /// </summary>
    /// <value>Часы.</value>
    /// <remarks>
    /// Действует ограниченное время (см. <see cref="TolokaActiveBuff.BuffUntil"/>).
    /// </remarks>
    public int BuffHours { get; set; }

    /// <summary>
    /// Продолжительность бонуса при следующем уровне «Сходни».
    /// </summary>
    /// <value>Часы.</value>
    /// <remarks>
    /// <see langword="null"/> – здание уже максимального уровня.
    /// </remarks>
    public int? NextBuffHours { get; set; }

    /// <summary>
    /// Кандидаты на следующую толоку с числом голосов – все типы справочника, включая текущий.
    /// </summary>
    /// <remarks>
    /// Победитель голосования сменяет взвешенный random при посеве следующей толоки (см. <see cref="TolokaManager.Contribute"/>).
    /// </remarks>
    public TolokaVoteCandidate[] Candidates { get; set; } = [];

    /// <summary>
    /// Тип толоки, за который проголосовал сам игрок в текущей инстанции.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – игрок ещё не голосовал.
    /// </remarks>
    public int? MyVoteTolokaTypeId { get; set; }
}

/// <summary>
/// Кандидат голосования за следующую толоку с накопленным числом голосов.
/// </summary>
public class TolokaVoteCandidate
{
    /// <summary>
    /// Тип толоки – ссылка на справочник <see cref="TolokaType.Id"/>.
    /// </summary>
    public int TolokaTypeId { get; set; }

    /// <summary>
    /// Отображаемое название типа толоки.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа толоки, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Число голосов за этот тип в текущей инстанции.
    /// </summary>
    public int Votes { get; set; }
}

/// <summary>
/// Один действующий бонус производству от недавно завершённой толоки.
/// </summary>
public class TolokaActiveBuff
{
    /// <summary>
    /// Технический код типа толоки, давшей бонус.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Название бонуса для отображения.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Прибавка к выходу производства.
    /// </summary>
    /// <value>Проценты.</value>
    public int Percent { get; set; }

    /// <summary>
    /// Момент, когда бонус перестанет действовать.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime BuffUntil { get; set; }
}
