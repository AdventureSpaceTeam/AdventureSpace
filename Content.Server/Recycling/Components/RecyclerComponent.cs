using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Content.Shared.Recycling;
using Content.Shared.Sound;
using Robust.Server.GameObjects;

namespace Content.Server.Recycling.Components
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [Friend(typeof(RecyclerSystem))]
    public sealed class RecyclerComponent : Component, ISuicideAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("safe")]
        internal bool Safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("efficiency")]
        internal float Efficiency = 0.25f;

        private void Clean()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        SuicideKind ISuicideAct.Suicide(EntityUid victim, IChatManager chat)
        {
            if (_entMan.TryGetComponent(victim, out ActorComponent? actor) &&
                actor.PlayerSession.ContentData()?.Mind is { } mind)
            {
                EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, false);
                mind.OwnedEntity?.PopupMessage(Loc.GetString("recycler-component-suicide-message"));
            }

            victim.PopupMessageOtherClients(Loc.GetString("recycler-component-suicide-message-others",
                ("victim", victim)));

            if (_entMan.TryGetComponent<SharedBodyComponent?>(victim, out var body))
            {
                body.Gib(true);
            }

            EntitySystem.Get<RecyclerSystem>().Bloodstain(this);
            return SuicideKind.Bloodloss;
        }

        /// <summary>
        /// Default sound to play when recycling
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")]
        public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/saw.ogg");

        // Ratelimit sounds to avoid spam
        public TimeSpan LastSound;
    }
}
