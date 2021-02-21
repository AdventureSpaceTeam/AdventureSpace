#nullable enable
using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry.ReagentDispenser;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Solutions
{
    [TestFixture]
    public sealed class ReagentDispenserTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestReagentDispenserInventory()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<ReagentDispenserInventoryPrototype>())
                {
                    foreach (var chem in proto.Inventory)
                    {
                        Assert.That(protoManager.HasIndex<ReagentPrototype>(chem), $"Unable to find chem {chem} in ReagentDispenserInventory {proto.ID}");
                    }
                }
            });
        }
    }
}
