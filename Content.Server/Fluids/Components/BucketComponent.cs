using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components
{
    /// <summary>
    /// Can a mop click on this entity and dump its fluids
    /// </summary>
    [RegisterComponent]
    public class BucketComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Bucket";
        public const string SolutionName = "bucket";

        private List<EntityUid> _currentlyUsing = new();

        public FixedPoint2 MaxVolume
        {
            get =>
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
                    ? solution.MaxVolume
                    : FixedPoint2.Zero;
            set
            {
                if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                {
                    solution.MaxVolume = value;
                }
            }
        }

        public FixedPoint2 CurrentVolume => EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution)
            ? solution.CurrentVolume
            : FixedPoint2.Zero;

        [DataField("sound")]
        private SoundSpecifier _sound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");


        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
            if (!solutionsSys.TryGetSolution(Owner, SolutionName, out var contents) ||
                _currentlyUsing.Contains(eventArgs.Using) ||
                !_entMan.TryGetComponent(eventArgs.Using, out MopComponent? mopComponent) ||
                mopComponent.Mopping)
            {
                return false;
            }

            if (CurrentVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("bucket-component-bucket-is-empty-message"));
                return false;
            }

            if (mopComponent.CurrentVolume == mopComponent.MaxVolume)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("bucket-component-mop-is-full-message"));
                return false;
            }

            _currentlyUsing.Add(eventArgs.Using);

            // IMO let em move while doing it.
            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, 1.0f, target: eventArgs.Target)
            {
                BreakOnStun = true,
                BreakOnDamage = true,
            };
            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterArgs);

            _currentlyUsing.Remove(eventArgs.Using);

            if (result == DoAfterStatus.Cancelled || _entMan.Deleted(Owner) || mopComponent.Deleted ||
                CurrentVolume <= 0 || !Owner.InRangeUnobstructed(mopComponent.Owner))
                return false;

            // Top up mops solution given it needs it to annihilate puddles I guess

            var transferAmount = FixedPoint2.Min(mopComponent.MaxVolume - mopComponent.CurrentVolume, CurrentVolume);
            if (transferAmount == 0)
            {
                return false;
            }

            var mopContents = mopComponent.MopSolution;

            if (mopContents == null)
            {
                return false;
            }

            var solution = solutionsSys.SplitSolution(Owner, contents, transferAmount);
            if (!solutionsSys.TryAddSolution(mopComponent.Owner, mopContents, solution))
            {
                return false;
            }

            SoundSystem.Play(Filter.Pvs(Owner), _sound.GetSound(), Owner);

            return true;
        }
    }
}
