namespace Domiki.Web.Village.Models;

/// <summary>
/// Вид погоды – вес в ротации и влияние на выход производства.
/// </summary>
/// <remarks>
/// Справочник наполняется через <see cref="Reference.ResourceManager.GetWeatherTypes"/>, используется в <see cref="WeatherManager"/>
/// и отдаётся на клиент как <see cref="Dto.WeatherPeriodDto"/>.
/// </remarks>
public class WeatherType
{
    /// <summary>
    /// Идентификатор типа погоды.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Вес типа погоды при случайной ротации.
    /// </summary>
    /// <remarks>
    /// Чем больше вес относительно суммы весов всех типов, тем выше шанс выпадения при досеивании расписания
    /// (см. <see cref="WeatherManager.EnsureWeatherSchedule"/>).
    /// </remarks>
    public int RotationWeight { get; set; }

    /// <summary>
    /// Влияние погоды на выход производства по типам построек.
    /// </summary>
    /// <remarks>
    /// Постройки без записи в массиве не затронуты.
    /// </remarks>
    public WeatherTypeEffect[] Effects { get; set; } = [];
}

/// <summary>
/// Влияние погоды на выход производства одного типа построек.
/// </summary>
public class WeatherTypeEffect
{
    /// <summary>
    /// Тип построек, на который действует эффект – ссылка на <see cref="Core.Models.DomikType.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Множитель выхода производства.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – без изменений. Диапазон эффектов ±25–50%.</value>
    /// <remarks>
    /// См. <see cref="WeatherManager.GetOutputPercent"/>.
    /// </remarks>
    public int OutputPercent { get; set; }
}
