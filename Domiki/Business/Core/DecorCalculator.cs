using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public static class DecorCalculator
    {
        public static int GetComfort(IEnumerable<PlayerDecor> playerDecors, DecorType[] decorTypes)
        {
            return playerDecors.Sum(decor => decor.Count * decorTypes.First(x => x.Id == decor.DecorTypeId).ComfortPoints);
        }
    }
}
