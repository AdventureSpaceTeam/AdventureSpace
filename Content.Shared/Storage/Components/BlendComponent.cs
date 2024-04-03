namespace Content.Shared.Storage;

using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class BlendComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("sound"), AutoNetworkedField]
    public SoundSpecifier? Sound = null;

    [Serializable, NetSerializable]
    public sealed class BlendComponentState : ComponentState
    {
        public bool Blending { get; init; }
    }

    [Serializable, NetSerializable]
    public enum BlendVisual
    {
        Visual,
        Normal,
        Blending
    }

}
