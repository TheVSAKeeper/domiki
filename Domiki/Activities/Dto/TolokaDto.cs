namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Активная (или только что завершённая) толока.
/// </summary>
/// <remarks>
/// Общесерверный проект, наполняемый вкладами всех игроков.
/// </remarks>
public class TolokaDto
{
    /// <summary>
    /// Идентификатор инстанции толоки.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Тип толоки – ссылка на справочник <see cref="Activities.Models.TolokaType.Id"/>.
    /// </summary>
    public int TolokaTypeId { get; set; }

    /// <summary>
    /// Отображаемое название толоки.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Ресурс, который собирают участники, – ссылка на <see cref="Reference.Dto.ResourceTypeDto.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Целевое значение общего счётчика, при достижении которого толока завершается.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Collected"/>.
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Сколько ресурса уже внесено всеми игроками суммарно.
    /// </summary>
    /// <remarks>
    /// Складывается из вкладов игроков (см. <see cref="TolokaStateDto.MyContribution"/> для вклада самого игрока); сравнивается с
    /// <see cref="Goal"/>.
    /// </remarks>
    public int Collected { get; set; }

    /// <summary>
    /// Момент начала текущей толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }
}

/// <summary>
/// Состояние толоки для конкретного игрока.
/// </summary>
/// <remarks>
/// Текущий проект (<see cref="Active"/>), вклад игрока (<see cref="MyContribution"/>) и действующие бонусы (<see cref="ActiveBuffs"/>).
/// </remarks>
public class TolokaStateDto
{
    /// <summary>
    /// Текущая активная толока.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – здание «Сходня» ещё не построено.
    /// </remarks>
    public TolokaDto Active { get; set; }

    /// <summary>
    /// Сколько ресурса внёс сам игрок в текущую толоку.
    /// </summary>
    /// <remarks>
    /// Входит в общий <see cref="TolokaDto.Collected"/>.
    /// </remarks>
    public int MyContribution { get; set; }

    /// <summary>
    /// Бонусы производству от недавно завершённых толок, всё ещё действующие игроку.
    /// </summary>
    /// <remarks>
    /// Пустой массив – бонусов нет.
    /// </remarks>
    public TolokaActiveBuffDto[] ActiveBuffs { get; set; }

    /// <summary>
    /// Продолжительность бонуса от завершения толоки – зависит от уровня «Сходни».
    /// </summary>
    /// <value>Часы.</value>
    /// <remarks>
    /// Действует ограниченное время (см. <see cref="TolokaActiveBuffDto.BuffUntil"/>).
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
}

/// <summary>
/// Один действующий бонус производству от недавно завершённой толоки.
/// </summary>
public class TolokaActiveBuffDto
{
    /// <summary>
    /// Технический код типа толоки, давшей бонус.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Название бонуса для отображения.
    /// </summary>
    public string Label { get; set; }

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
