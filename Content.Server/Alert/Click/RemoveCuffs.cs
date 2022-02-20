using Content.Server.Cuffs.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    ///     Try to remove handcuffs from yourself
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class RemoveCuffs : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out CuffableComponent? cuffableComponent))
            {
                cuffableComponent.TryUncuff(player);
            }
        }
    }
}
