namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Активная (или только что завершённая) толока.
/// </summary>
/// <remarks>
/// Общесерверный проект, наполняемый вкладами всех игроков.
/// </remarks>
public sealed record TolokaDto
{
    /// <summary>
    /// Идентификатор инстанции толоки.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Тип толоки – ссылка на справочник <see cref="Activities.Models.TolokaType.Id"/>.
    /// </summary>
    public required int TolokaTypeId { get; init; }

    /// <summary>
    /// Отображаемое название толоки.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Позиции корзины сбора этой инстанции – цель, собрано и вклад самого игрока по каждому ресурсу.
    /// </summary>
    public required TolokaPositionDto[] Positions { get; init; }

    /// <summary>
    /// Момент начала текущей толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime StartDate { get; init; }
}

/// <summary>
/// Одна позиция корзины сбора инстанции толоки.
/// </summary>
public sealed record TolokaPositionDto
{
    /// <summary>
    /// Ресурс позиции, – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public required int ResourceTypeId { get; init; }

    /// <summary>
    /// Целевое количество ресурса, при достижении которого позиция считается набранной.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Collected"/>.
    /// </remarks>
    public required int Goal { get; init; }

    /// <summary>
    /// Сколько ресурса уже внесено всеми игроками по этой позиции.
    /// </summary>
    /// <remarks>
    /// Складывается из вкладов игроков (см. <see cref="MyContribution"/> для вклада самого игрока); сравнивается с
    /// <see cref="Goal"/>.
    /// </remarks>
    public required int Collected { get; init; }

    /// <summary>
    /// Сколько ресурса внёс сам игрок в эту позицию.
    /// </summary>
    /// <remarks>
    /// Входит в общий <see cref="Collected"/>.
    /// </remarks>
    public required int MyContribution { get; init; }
}

/// <summary>
/// Состояние толоки для конкретного игрока.
/// </summary>
/// <remarks>
/// Текущий проект (<see cref="Active"/>) и действующие бонусы (<see cref="ActiveBuffs"/>); вклад игрока – в позициях
/// <see cref="TolokaDto.Positions"/> (см. <see cref="TolokaPositionDto.MyContribution"/>).
/// </remarks>
public sealed record TolokaStateDto
{
    /// <summary>
    /// Текущая активная толока.
    /// </summary>
    public required TolokaDto Active { get; init; }

    /// <summary>
    /// Бонусы производству от недавно завершённых толок, всё ещё действующие игроку.
    /// </summary>
    /// <remarks>
    /// Пустой массив – бонусов нет.
    /// </remarks>
    public required TolokaActiveBuffDto[] ActiveBuffs { get; init; }

    /// <summary>
    /// Продолжительность бонуса от завершения толоки – зависит от уровня «Сходни».
    /// </summary>
    /// <value>Часы.</value>
    /// <remarks>
    /// Действует ограниченное время (см. <see cref="TolokaActiveBuffDto.BuffUntil"/>).
    /// </remarks>
    public required int BuffHours { get; init; }

    /// <summary>
    /// Продолжительность бонуса при следующем уровне «Сходни».
    /// </summary>
    /// <value>Часы.</value>
    /// <remarks>
    /// <see langword="null"/> – здание уже максимального уровня.
    /// </remarks>
    public int? NextBuffHours { get; init; }

    /// <summary>
    /// Кандидаты на следующую толоку с числом голосов – все типы справочника, включая текущий.
    /// </summary>
    /// <remarks>
    /// Победитель голосования сменяет взвешенный random при посеве следующей толоки.
    /// </remarks>
    public required TolokaVoteCandidateDto[] Candidates { get; init; }

    /// <summary>
    /// Тип толоки, за который проголосовал сам игрок в текущей инстанции.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – игрок ещё не голосовал.
    /// </remarks>
    public int? MyVoteTolokaTypeId { get; init; }
}

/// <summary>
/// Кандидат голосования за следующую толоку с накопленным числом голосов.
/// </summary>
public sealed record TolokaVoteCandidateDto
{
    /// <summary>
    /// Тип толоки – ссылка на справочник <see cref="Activities.Models.TolokaType.Id"/>.
    /// </summary>
    public required int TolokaTypeId { get; init; }

    /// <summary>
    /// Отображаемое название типа толоки.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Технический код типа толоки, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Число голосов за этот тип в текущей инстанции.
    /// </summary>
    public required int Votes { get; init; }
}

/// <summary>
/// Один действующий бонус производству от недавно завершённой толоки.
/// </summary>
public sealed record TolokaActiveBuffDto
{
    /// <summary>
    /// Технический код типа толоки, давшей бонус.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Название бонуса для отображения.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Прибавка к выходу производства.
    /// </summary>
    /// <value>Проценты.</value>
    public required int Percent { get; init; }

    /// <summary>
    /// Момент, когда бонус перестанет действовать.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime BuffUntil { get; init; }
}
