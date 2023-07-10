using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Tools.Components;

namespace Content.IntegrationTests.Tests.Weldable;

/// <summary>
///  Simple test to check that using a welder on a locker will weld it shut.
/// </summary>
public sealed class WeldableTests : InteractionTest
{
    public const string Locker = "LockerFreezer";

    [Test]
    public async Task WeldLocker()
    {
        await SpawnTarget(Locker);
        var comp = Comp<WeldableComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.Weldable, Is.True);
            Assert.That(comp.IsWelded, Is.False);
        });

        await Interact(Weld);
        Assert.That(comp.IsWelded, Is.True);
        AssertPrototype(Locker); // Prototype did not change.
    }
}
