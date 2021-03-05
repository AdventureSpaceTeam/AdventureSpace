#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class JobSpecial
    {
        public virtual void AfterEquip(IEntity mob)
        {

        }
    }
}
