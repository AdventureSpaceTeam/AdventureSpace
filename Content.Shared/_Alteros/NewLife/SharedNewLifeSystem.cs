using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Serialization;

namespace Content.Shared.NewLife
{
    public abstract class SharedNewLifeSystem : EntitySystem
    {
        [Dependency] protected readonly SharedPopupSystem Popup = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

    }

    [Serializable, NetSerializable]
    public sealed class NewLifeOpenRequest : EntityEventArgs
    {
    }


    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class NewLifeUserData
    {
        public TimeSpan NextAllowRespawn { get; set; }
        public List<int> UsedCharactersForRespawn { get; set; } = new();
    }
}
