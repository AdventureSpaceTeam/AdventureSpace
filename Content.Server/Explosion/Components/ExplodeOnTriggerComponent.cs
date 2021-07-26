using Robust.Shared.GameObjects;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Explode using the entity's <see cref="ExplosiveComponent"/> if Triggered.
    /// </summary>
    [RegisterComponent]
    public class ExplodeOnTriggerComponent : Component
    {
        public override string Name => "ExplodeOnTrigger";
    }
}
