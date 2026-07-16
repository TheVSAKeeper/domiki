using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

public class StarterGoal
{
    [Key]
    public int Id { get; set; }

    public int Ordinal { get; set; }

    [MaxLength(200)]
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    public GoalConditionType ConditionType { get; set; }

    public int Param { get; set; }

    public int Param2 { get; set; }

    public int RewardCoins { get; set; }
}
