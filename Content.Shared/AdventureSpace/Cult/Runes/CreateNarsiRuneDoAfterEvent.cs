using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Cult.Runes;

[Serializable, NetSerializable]
public sealed partial class CreateNarsiRuneDoAfterEvent : DoAfterEvent
{
    [DataField("prototype", required: true)]
    public string Prototype = default!;

    public CreateNarsiRuneDoAfterEvent()
    {
    }
    public CreateNarsiRuneDoAfterEvent(string prototype)
    {
        Prototype = prototype;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
