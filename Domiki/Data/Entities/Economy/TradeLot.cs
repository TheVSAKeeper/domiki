using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Лот обмена ресурсами на Торговом дворе между игроками.
/// </summary>
[Table("TradeLots")]
public class TradeLot
{
    /// <summary>
    /// Идентификатор лота.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, выставивший лот.
    /// </summary>
    public int SellerId { get; set; }

    /// <summary>
    /// Тип ресурса, который продавец отдаёт покупателю.
    /// </summary>
    public int GiveResourceTypeId { get; set; }

    /// <summary>
    /// Количество отдаваемого ресурса.
    /// </summary>
    public int GiveValue { get; set; }

    /// <summary>
    /// Тип ресурса, который продавец хочет получить взамен.
    /// </summary>
    public int WantResourceTypeId { get; set; }

    /// <summary>
    /// Количество требуемого ресурса.
    /// </summary>
    public int WantValue { get; set; }

    /// <summary>
    /// Комиссия в монетах, списанная с продавца при выставлении лота.
    /// </summary>
    /// <value>Монеты.</value>
    /// <remarks>
    /// Зависит от уровня Торгового двора; при отмене или истечении лота не возвращается.
    /// </remarks>
    public int CommissionCoins { get; set; }

    /// <summary>
    /// Момент выставления лота.
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Момент истечения лота.
    /// </summary>
    /// <remarks>
    /// Если обмен никто не принял, планировщик <see cref="Core.Scheduling.Calculator"/> возвращает продавцу <see cref="GiveValue"/> ресурса.
    /// </remarks>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// Навигационное свойство к продавцу из <see cref="SellerId"/>.
    /// </summary>
    public Player Seller { get; set; }
}
