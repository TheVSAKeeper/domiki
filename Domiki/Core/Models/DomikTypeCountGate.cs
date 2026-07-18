namespace Domiki.Web.Core.Models;

/// <summary>
/// Порог обжитости деревни, открывающий постройку очередного экземпляра домика этого типа.
/// </summary>
/// <remarks>
/// Модель-зеркало сущности <see cref="Data.Entities.DomikTypeCountGate"/>, загружается целиком (см.
/// <see cref="Reference.ResourceManager.GetDomikTypeCountGates"/>) и используется в <see cref="DomikManager.GetPurchaseAvailableDomiks"/>.
/// </remarks>
public class DomikTypeCountGate
{
    /// <summary>
    /// Тип домика, для которого задан порог – ссылка на <see cref="DomikType.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Порядковый номер экземпляра домика этого типа.
    /// </summary>
    /// <remarks>
    /// Нумерация начинается с <c>2</c>, <c>3</c> и так далее – начиная с этого номера действует порог.
    /// </remarks>
    public int Ordinal { get; set; }

    /// <summary>
    /// Обжитость деревни, необходимая, чтобы построить <see cref="Ordinal"/>-й экземпляр домика этого типа.
    /// </summary>
    public int UnlockLevel { get; set; }
}
