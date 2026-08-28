namespace Domiki.Web.Data.Entities;

/// <summary>
/// Группа событий журнала – единица фильтрации вместо двух десятков отдельных типов.
/// </summary>
/// <remarks>
/// Каноническая карта <see cref="PlayerEventType"/> → группа живёт на сервере
/// (<see cref="Infrastructure.PlayerEventGroups"/>): прежде принадлежность вычислялась на клиенте цветом записи,
/// поэтому отфильтровать выдачу на сервере было нечем. Словарь групп повторяет вкладки игры.
/// </remarks>
public enum PlayerEventGroup
{
    /// <summary>
    /// Группа не определена.
    /// </summary>
    None = 0,

    /// <summary>
    /// Хозяйство: производство и его срывы.
    /// </summary>
    Household = 1,

    /// <summary>
    /// Трудяги: кормёжка, вехи, происшествия с работниками.
    /// </summary>
    Workers = 2,

    /// <summary>
    /// Деревня: постройки, наказы, экспедиции, толока, переезд.
    /// </summary>
    Village = 3,

    /// <summary>
    /// Гости: соседи, гостинцы, книга гостей, поручения.
    /// </summary>
    Guests = 4,

    /// <summary>
    /// Ярмарка: торговые лоты.
    /// </summary>
    Market = 5,
}
