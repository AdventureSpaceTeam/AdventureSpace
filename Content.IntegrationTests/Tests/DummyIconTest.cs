#nullable enable
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class DummyIconTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var client = StartClient();
            await client.WaitIdleAsync();

            var prototypeManager = client.ResolveDependency<IPrototypeManager>();
            var resourceCache = client.ResolveDependency<IResourceCache>();

            await client.WaitAssertion(() =>
            {
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.Components.ContainsKey("Sprite")) continue;

                    var _ = SpriteComponent.GetPrototypeTextures(proto, resourceCache).ToList();
                }
            });
        }
    }
}
