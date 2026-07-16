using Domiki.Web.Business.Core;
using Domiki.Web.Data;

namespace Domiki.Web.Tests
{
    public class SoftlockInvariantTests : TestBase
    {
        private const int ClayMineDomikId = 2;
        private const int ClayDigReceiptId = 1;
        private const int CoinResourceTypeId = 1;
        private const int ClayResourceTypeId = 4;

        /// <summary>
        /// Игрок без монет не попадает в софтлок: копка глины не требует монет, а сдача заказа на глину возвращает монеты в оборот.
        /// </summary>
        [Test]
        public void PlayerWithoutCoinsCanDigClayAndCompleteAffordableOrderForCoinsTest()
        {
            var playerId = CreatePlayer();
            SetResourceValue(playerId, CoinResourceTypeId, 0);

            Assert.DoesNotThrow(() => StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId));
            Assert.That(GetResourceValue(playerId, ClayResourceTypeId), Is.GreaterThan(0));

            var order = GetOrders(playerId).First(x => x.Resources.Single().Type.Id == ClayResourceTypeId);
            var need = order.Resources.Single();
            SetResourceValue(playerId, CoinResourceTypeId, 0);
            EnsureResourceAtLeast(playerId, need.Type.Id, need.Value);

            CompleteOrder(playerId, order.Id);

            Assert.That(GetResourceValue(playerId, CoinResourceTypeId), Is.GreaterThan(0));
        }

        private int CreatePlayer()
        {
            using var uow = GetUow();
            var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).StartManufacture(playerId, domikId, receiptId);
            uow.Commit();
        }

        private Domiki.Web.Business.Models.Order[] GetOrders(int playerId)
        {
            using var uow = GetUow();
            var orders = GetOrderManager(uow).GetOrders(playerId).ToArray();
            uow.Commit();
            return orders;
        }

        private void CompleteOrder(int playerId, int orderId)
        {
            using var uow = GetUow();
            GetOrderManager(uow).CompleteOrder(playerId, orderId);
            uow.Commit();
        }

        private int GetResourceValue(int playerId, int typeId)
        {
            using var uow = GetUow();
            return uow.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId).Value;
        }

        private void SetResourceValue(int playerId, int typeId, int value)
        {
            using var uow = GetUow();
            var resource = uow.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId);
            resource.Value = value;
            uow.Commit();
        }

        private void EnsureResourceAtLeast(int playerId, int typeId, int value)
        {
            using var uow = GetUow();
            var resource = uow.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId);
            resource.Value = Math.Max(resource.Value, value);
            uow.Commit();
        }
    }
}
