using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("TradeLots")]
public class TradeLot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int SellerId { get; set; }

    public int GiveResourceTypeId { get; set; }

    public int GiveValue { get; set; }

    public int WantResourceTypeId { get; set; }

    public int WantValue { get; set; }

    public int CommissionCoins { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime ExpireDate { get; set; }

    public Player Seller { get; set; }
}
