namespace Domiki.Web.Workers;

public static class WorkerSkillCalculator
{
    public const double MaxBonus = 0.15;
    public const double Tau = 10.0;

    public static int GetBonusPercent(int uses)
    {
        if (uses <= 0)
        {
            return 0;
        }

        return (int)Math.Round(MaxBonus * (1 - Math.Exp(-uses / Tau)) * 100);
    }
}
