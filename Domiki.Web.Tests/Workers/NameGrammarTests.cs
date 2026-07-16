using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

[TestFixture]
public sealed class NameGrammarTests
{
    /// <summary>
    /// Грамматический род формы глагола определяется по имени трудяги: мужское имя даёт мужское окончание, женское – женское.
    /// Неизвестное имя считается мужским.
    /// </summary>
    /// <param name="name">Имя трудяги.</param>
    /// <param name="expected">Ожидаемая форма глагола по роду имени.</param>
    [TestCase("Дарья", "сделала")]
    [TestCase("Злата", "сделала")]
    [TestCase("Пелагея", "сделала")]
    [TestCase("Лукерья", "сделала")]
    [TestCase("Егор", "сделал")]
    [TestCase("Борис", "сделал")]
    [TestCase("Илья", "сделал")]
    [TestCase("Сава", "сделал")]
    [TestCase("Трудяга Безымянный", "сделал")]
    public void GenderForm_PicksFormByWorkerGender(string name, string expected)
    {
        Assert.That(NameGrammar.GenderForm(name, "сделал", "сделала"), Is.EqualTo(expected));
    }

    /// <summary>
    /// Каждое имя из женского списка NameGrammar.Names даёт женскую форму согласования без исключений.
    /// </summary>
    [Test]
    public void GenderForm_RoutesEveryFeminineNameToFeminineForm()
    {
        var feminine = NameGrammar.Names.Where(name => NameGrammar.GenderOf(name) == WorkerGender.Feminine).ToArray();
        Assert.That(feminine, Is.Not.Empty);
        Assert.That(feminine.Select(name => NameGrammar.GenderForm(name, "m", "f")), Is.All.EqualTo("f"));
    }
}
