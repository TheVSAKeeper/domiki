using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public static class Throws
{
    public static BusinessException Business(Action act)
    {
        var ex = Assert.Throws<BusinessException>(act);
        Assert.That(ex, Is.Not.Null);
        return ex!;
    }
}
