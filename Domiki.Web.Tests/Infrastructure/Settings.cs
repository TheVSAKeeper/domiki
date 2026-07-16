namespace Domiki.Web.Tests;

public sealed class Settings
{
    public ConnectionStringsValue ConnectionStrings { get; set; }

    public sealed class ConnectionStringsValue
    {
        public string DefaultConnection { get; set; }
    }
}
