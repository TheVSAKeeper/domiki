namespace Domiki.Web.Tests;

public sealed class Settings
{
    public required ConnectionStringsValue ConnectionStrings { get; set; }

    public sealed class ConnectionStringsValue
    {
        public required string DefaultConnection { get; set; }
    }
}
