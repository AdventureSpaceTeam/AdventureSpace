namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Buildings;

[RegisterComponent]
public sealed partial class SharedFakeNarsiDoorComponent : Component
{
    [DataField("fakeSprite", required: true)]
    public string FakeRsiPath = default!;

    [DataField("realSprite", required: true)]
    public string RealRsiPath = default!;
}
