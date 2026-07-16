
namespace Domiki.Web.Workers
{
    public enum WorkerGender
    {
        None = 0,
        Masculine = 1,
        Feminine = 2,
    }

    public static class NameGrammar
    {
        private static readonly (string Name, WorkerGender Gender, string Genitive)[] Entries =
        {
            ("Аким", WorkerGender.Masculine, "Акима"),
            ("Борис", WorkerGender.Masculine, "Бориса"),
            ("Варвара", WorkerGender.Feminine, "Варвары"),
            ("Глеб", WorkerGender.Masculine, "Глеба"),
            ("Дарья", WorkerGender.Feminine, "Дарьи"),
            ("Егор", WorkerGender.Masculine, "Егора"),
            ("Ждан", WorkerGender.Masculine, "Ждана"),
            ("Злата", WorkerGender.Feminine, "Златы"),
            ("Илья", WorkerGender.Masculine, "Ильи"),
            ("Кира", WorkerGender.Feminine, "Киры"),
            ("Лада", WorkerGender.Feminine, "Лады"),
            ("Мирон", WorkerGender.Masculine, "Мирона"),
            ("Нина", WorkerGender.Feminine, "Нины"),
            ("Остап", WorkerGender.Masculine, "Остапа"),
            ("Пелагея", WorkerGender.Feminine, "Пелагеи"),
            ("Роман", WorkerGender.Masculine, "Романа"),
            ("Сава", WorkerGender.Masculine, "Савы"),
            ("Тая", WorkerGender.Feminine, "Таи"),
            ("Ульяна", WorkerGender.Feminine, "Ульяны"),
            ("Фёдор", WorkerGender.Masculine, "Фёдора"),
            ("Ярина", WorkerGender.Feminine, "Ярины"),
            ("Агата", WorkerGender.Feminine, "Агаты"),
            ("Бажен", WorkerGender.Masculine, "Бажена"),
            ("Велена", WorkerGender.Feminine, "Велены"),
            ("Гордей", WorkerGender.Masculine, "Гордея"),
            ("Демьян", WorkerGender.Masculine, "Демьяна"),
            ("Есения", WorkerGender.Feminine, "Есении"),
            ("Захар", WorkerGender.Masculine, "Захара"),
            ("Лукерья", WorkerGender.Feminine, "Лукерьи"),
            ("Марта", WorkerGender.Feminine, "Марты"),
            ("Назар", WorkerGender.Masculine, "Назара"),
            ("Прасковья", WorkerGender.Feminine, "Прасковьи"),
        };

        public static readonly string[] Names = Entries.Select(x => x.Name).ToArray();

        private static readonly Dictionary<string, (WorkerGender Gender, string Genitive)> ByName =
            Entries.ToDictionary(x => x.Name, x => (x.Gender, x.Genitive));

        public static WorkerGender GenderOf(string name)
        {
            return ByName.TryGetValue(name, out var entry) ? entry.Gender : WorkerGender.Masculine;
        }

        public static string GenderForm(string name, string masculine, string feminine)
        {
            return GenderOf(name) == WorkerGender.Feminine ? feminine : masculine;
        }

        public static string Genitive(string name)
        {
            if (ByName.TryGetValue(name, out var entry))
            {
                return entry.Genitive;
            }

            if (name.StartsWith("Трудяга ", StringComparison.Ordinal))
            {
                return "Трудяги " + name["Трудяга ".Length..];
            }

            return name;
        }
    }
}
