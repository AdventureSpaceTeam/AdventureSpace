using System.Threading.Tasks;
using Content.Shared.VendingMachines;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(VendingMachineInventoryPrototype))]
    public sealed class VendingMachineTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();
            
            server.Assert(() =>
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                foreach (var vendorProto in prototypeManager.EnumeratePrototypes<VendingMachineInventoryPrototype>())
                {
                    foreach (var (item, _) in vendorProto.StartingInventory)
                    {
                        try
                        {
                            prototypeManager.Index<EntityPrototype>(item);
                        }
                        catch (UnknownPrototypeException)
                        {
                            throw new UnknownPrototypeException($"Unknown prototype {item} on vending inventory {vendorProto.Name}");
                        }
                    }
                }
            });

            await server.WaitIdleAsync();
        }
    }
}