using Robust.Shared.GameObjects;

namespace Content.Server.Sound
{
    /// <summary>
    /// Simple sound emitter that emits sound on ActivateInWorld
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnActivateComponent : BaseEmitSoundComponent
    {
        /// <inheritdoc />
        public override string Name => "EmitSoundOnActivate";
    }
}
