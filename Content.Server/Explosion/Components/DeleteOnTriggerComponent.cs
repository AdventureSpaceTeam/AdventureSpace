using Robust.Shared.GameObjects;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Will delete the attached entity upon a <see cref="TriggerEvent"/>.
    /// </summary>
    [RegisterComponent]
    public class DeleteOnTriggerComponent : Component
    {
        public override string Name => "DeleteOnTrigger";
    }
}
