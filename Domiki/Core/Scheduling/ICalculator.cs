namespace Domiki.Web.Core.Scheduling;

public interface ICalculator
{
    void CheckInit();
    void Insert(CalculateInfo cData);
    void Remove(int playerId, long objectId, CalculateTypes type);
    void Reschedule(int playerId, long objectId, CalculateTypes type, DateTime newDate);
}
