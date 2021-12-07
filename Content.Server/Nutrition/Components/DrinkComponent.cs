using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using System.Threading;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    [Friend(typeof(DrinkSystem))]
    public class DrinkComponent : Component
    {
        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
        public const string DefaultSolutionName = "drink";

        public override string Name => "Drink";

        [ViewVariables]
        [DataField("useSound")]
        public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

        [ViewVariables]
        [DataField("isOpen")]
        internal bool DefaultToOpened;

        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; [UsedImplicitly] private set; } = FixedPoint2.New(5);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Opened;

        [DataField("openSounds")]
        public SoundSpecifier OpenSounds = new SoundCollectionSpecifier("canOpenSounds");

        [DataField("pressurized")]
        public bool Pressurized;

        [DataField("burstSound")]
        public SoundSpecifier BurstSound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");

        /// <summary>
        ///     This is how many seconds it takes to force feed someone this drink.
        /// </summary>
        [DataField("forceFeedDelay")]
        public float ForceFeedDelay = 3;

        /// <summary>
        ///     Token for interrupting a do-after action (e.g., force feeding). If not null, implies component is
        ///     currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;
    }
}
