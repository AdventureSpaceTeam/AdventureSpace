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
                    if (proto.Abstract || !proto.Components.ContainsKey("Sprite")) continue;

                    Assert.DoesNotThrow(() =>
                    {
                        var _ = SpriteComponent.GetPrototypeTextures(proto, resourceCache).ToList();
                    }, "Prototype {0} threw an exception when getting its textures.",
                        proto.ID);
                }
            });
        }
    }
}
