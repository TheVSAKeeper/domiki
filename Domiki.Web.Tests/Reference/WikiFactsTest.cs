using System.Text;
using System.Text.RegularExpressions;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class WikiFactsTest
{
    private static readonly Regex TokenPattern = new(@"\{([a-zA-Z][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);

    private static readonly Regex TierPattern = new(@"from: (?<from>\d+), name: '(?<name>[^']+)'", RegexOptions.Compiled);

    private static readonly Regex FallbackPattern = new(@"^    (?<key>[a-zA-Z][a-zA-Z0-9_]*): '(?<value>[^']*)',$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Каждая подстановка в статьях справочника разрешается фактом бэкенда: числа в тексте статьи берутся из игровых
    /// констант и справочных данных, а не набираются руками.
    /// </summary>
    [Test]
    public void WikiTokensResolveTest()
    {
        var facts = GetFacts();
        var missing = GetTokens().Where(token => !facts.ContainsKey(token)).Order().ToArray();

        Assert.That(missing, Is.Empty, "Подстановки без факта: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Каждый факт справочника подставлен хотя бы в одну статью: осиротевший факт значит, что утверждение переписали
    /// и число молча отцепилось от кода.
    /// </summary>
    [Test]
    public void WikiFactsAreUsedTest()
    {
        var tokens = GetTokens().ToHashSet();
        var unused = GetFacts().Keys.Where(key => !tokens.Contains(key)).Order().ToArray();

        Assert.That(unused, Is.Empty, "Факты без подстановки: " + string.Join(", ", unused));
    }

    /// <summary>
    /// Ступени, которыми сосед зовёт игрока, стоят на тех же порогах доброго имени, что и статья справочника.
    /// </summary>
    /// <param name="tierName">Как сосед зовёт игрока на этой ступени.</param>
    /// <param name="factKey">Имя факта с порогом этой ступени.</param>
    [TestCase("знакомец", "convoyAccessReputation")]
    [TestCase("в доверии", "convoySecondGoodReputation")]
    [TestCase("свой", "giftBonusReputation")]
    [TestCase("родня", "convoyHighLimitReputation")]
    public void ReputationTiersMatchFactsTest(string tierName, string factKey)
    {
        var tiers = TierPattern.Matches(ReadUtil("reputationTiers.ts"))
            .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["from"].Value);

        Assert.That(tiers[tierName], Is.EqualTo(GetFacts()[factKey]));
    }

    /// <summary>
    /// Запасной словарь фактов во фронте совпадает с тем, что отдаёт бэкенд: без сети справочник поднимается из него,
    /// и разошедшийся снимок показал бы игроку числа, которых в игре уже нет.
    /// </summary>
    /// <remarks>
    /// Перезаписать снимок после правки констант или справочных данных – тестом <see cref="RewriteFallbackTest"/>.
    /// </remarks>
    [Test]
    public void FallbackMatchesFactsTest()
    {
        Assert.That(ReadFallback(), Is.EqualTo(GetFacts()));
    }

    /// <summary>
    /// Перезаписывает запасной словарь фактов во фронте по текущим константам и справочным данным.
    /// </summary>
    [Test]
    [Explicit]
    public void RewriteFallbackTest()
    {
        File.WriteAllText(FallbackPath(), BuildFallbackSource(GetFacts()), new UTF8Encoding(false));
    }

    private static string BuildFallbackSource(Dictionary<string, string> facts)
    {
        var quoted = facts.Where(fact => fact.Value.Contains('\'') || fact.Value.Contains('\\')).Select(fact => fact.Key).ToArray();
        Assert.That(quoted, Is.Empty, "Факты с кавычкой или слешем в значении не переносятся в запасной словарь: " + string.Join(", ", quoted));

        var lines = facts.OrderBy(fact => fact.Key, StringComparer.Ordinal)
            .Select(fact => $"    {fact.Key}: '{fact.Value}',");

        return "export const wikiFactsFallback: Readonly<Record<string, string>> = {\n"
            + string.Join("\n", lines)
            + "\n};\n";
    }

    private static Dictionary<string, string> ReadFallback()
    {
        return FallbackPattern.Matches(File.ReadAllText(FallbackPath()))
            .ToDictionary(match => match.Groups["key"].Value, match => match.Groups["value"].Value);
    }

    private static string FallbackPath()
    {
        return Path.Combine(GetRepositoryRoot(), "Domiki", "ClientApp", "src", "utils", "wikiFactsFallback.ts");
    }

    private static Dictionary<string, string> GetFacts()
    {
        return App.Act<WikiFactsProvider, Dictionary<string, string>>(provider => provider.GetFacts());
    }

    private static string[] GetTokens()
    {
        return TokenPattern.Matches(ReadUtil("wikiTexts.ts"))
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();
    }

    private static string ReadUtil(string fileName)
    {
        return File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Domiki", "ClientApp", "src", "utils", fileName));
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Domiki.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Не найден корень репозитория – каталог с Domiki.sln");
    }
}
