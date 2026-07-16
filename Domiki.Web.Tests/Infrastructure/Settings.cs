namespace Domiki.Web.Tests
{
    public class Settings
    {
        public ConnectionStringsValue ConnectionStrings { get; set; }

        public class ConnectionStringsValue
        {
            public string DefaultConnection { get; set; }
        }
    }
}
