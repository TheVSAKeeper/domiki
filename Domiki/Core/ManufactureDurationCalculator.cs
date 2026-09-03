using Domiki.Web.Core.Models;

namespace Domiki.Web.Core;

/// <summary>
/// Считает, сколько идёт смена: черты и навыки отобранных трудяг, уклад деревни, перк «Долгая привычка», нижний
/// порог и рвение.
/// </summary>
/// <remarks>
/// Вынесен из <see cref="DomikManager.StartManufacture"/> ради второго исполнителя: клиент рисует таймер запущенной
/// смены до ответа сервера и обязан получить ту же секунду. Совпадение закрыто вектором –
/// <c>Domiki.Web.Tests/Core/ManufactureDurationVectorTest.cs</c> сверяет этот расчёт с
/// <c>ClientApp/src/utils/manufactureDuration.ts</c> через общий файл случаев.
/// Порядок множителей значим: каждый шаг округляется вверх по отдельности, поэтому переставленные шаги дают другую
/// секунду.
/// </remarks>
public static class ManufactureDurationCalculator
{
    /// <summary>
    /// Считает длительность смены.
    /// </summary>
    /// <param name="input">Исходные данные расчёта.</param>
    /// <returns>Длительность в секундах и признак списанного заряда рвения.</returns>
    public static ManufactureDurationResult Compute(ManufactureDurationInput input)
    {
        var duration = input.ReceiptDurationSeconds;

        var averageSpeedupPercent = -input.TraitDurationPercents.Average();
        duration = (int)Math.Ceiling(duration * (100 - averageSpeedupPercent) / 100);

        var averageSkillPercent = input.SkillBonusPercents.Average();
        duration = (int)Math.Ceiling(duration * (100 - averageSkillPercent) / 100);

        duration = (int)Math.Ceiling(duration * input.ProfilePercent / 100.0);
        duration = (int)Math.Ceiling(duration * input.PerkPercent / 100.0);

        duration = Math.Max(duration, (int)Math.Ceiling(input.ReceiptDurationSeconds * DomikManager.MinDurationShare));

        var zealAllowed = input.ReceiptDurationSeconds <= DomikManager.ZealMaxRecipeSeconds && !input.IsMarketDomik;
        if (!zealAllowed || input.ZealCharges <= 0)
        {
            return new() { Seconds = duration, ZealSpent = false };
        }

        var multiplier = input.ZealCharges > DomikManager.ZealX4Threshold ? 4 : 2;
        return new() { Seconds = Math.Max(1, duration / multiplier), ZealSpent = true };
    }
}
