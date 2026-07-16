using Domiki.Web.Business.Core;

namespace Domiki.Web.Tests
{
    [TestFixture]
    public class NameGrammarTests
    {
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

        [Test]
        public void GenderForm_RoutesEveryFeminineNameToFeminineForm()
        {
            var feminine = NameGrammar.Names.Where(name => NameGrammar.GenderOf(name) == WorkerGender.Feminine).ToArray();
            Assert.That(feminine, Is.Not.Empty);
            Assert.That(feminine.Select(name => NameGrammar.GenderForm(name, "m", "f")), Is.All.EqualTo("f"));
        }
    }
}
