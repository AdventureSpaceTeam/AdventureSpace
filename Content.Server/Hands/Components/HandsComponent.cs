using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Act;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Shared.Audio;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Hands.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
#pragma warning disable 618
    public class HandsComponent : SharedHandsComponent, IBodyPartAdded, IBodyPartRemoved, IDisarmedAct
#pragma warning restore 618
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        [DataField("disarmedSound")] SoundSpecifier _disarmedSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        int IDisarmedAct.Priority => int.MaxValue; // We want this to be the last disarm act to run.

        #region Pull/Disarm

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            // If this annoys you, which it should.
            // Ping Smugleaf.
            var location = args.Part.Symmetry switch
            {
                BodyPartSymmetry.None => HandLocation.Middle,
                BodyPartSymmetry.Left => HandLocation.Left,
                BodyPartSymmetry.Right => HandLocation.Right,
                _ => throw new ArgumentOutOfRangeException()
            };

            AddHand(args.Slot, location);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            RemoveHand(args.Slot);
        }

        bool IDisarmedAct.Disarmed(DisarmedActEvent @event)
        {
            if (BreakPulls())
                return false;

            var source = @event.Source;
            var target = @event.Target;

            SoundSystem.Play(Filter.Pvs(source), _disarmedSound.GetSound(), source, AudioHelpers.WithVariation(0.025f));

            if (ActiveHand != null && Drop(ActiveHand, false))
            {
                source.PopupMessageOtherClients(Loc.GetString("hands-component-disarm-success-others-message", ("disarmer", _entities.GetComponent<MetaDataComponent>(source).EntityName), ("disarmed", _entities.GetComponent<MetaDataComponent>(target).EntityName)));
                source.PopupMessageCursor(Loc.GetString("hands-component-disarm-success-message", ("disarmed", _entities.GetComponent<MetaDataComponent>(target).EntityName)));
            }
            else
            {
                source.PopupMessageOtherClients(Loc.GetString("hands-component-shove-success-others-message", ("shover", _entities.GetComponent<MetaDataComponent>(source).EntityName), ("shoved", _entities.GetComponent<MetaDataComponent>(target).EntityName)));
                source.PopupMessageCursor(Loc.GetString("hands-component-shove-success-message", ("shoved", _entities.GetComponent<MetaDataComponent>(target).EntityName)));
            }

            return true;
        }

        private bool BreakPulls()
        {
            // What is this API??
            if (!_entities.TryGetComponent(Owner, out SharedPullerComponent? puller)
                || puller.Pulling is not {Valid: true} pulling || !_entities.TryGetComponent(puller.Pulling.Value, out SharedPullableComponent? pullable))
                return false;

            return _entitySystemManager.GetEntitySystem<PullingSystem>().TryStopPull(pullable);
        }

        #endregion

        #region Old public methods

        public IEnumerable<string> HandNames => Hands.Select(h => h.Name);

        public int Count => Hands.Count;

        /// <summary>
        ///     Returns a list of all hand names, with the active hand being first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
                yield return ActiveHand;

            foreach (var hand in Hands)
            {
                if (hand.Name == ActiveHand)
                    continue;

                yield return hand.Name;
            }
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public SharedItemComponent? GetItem(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return null;

            _entities.TryGetComponent(heldEntity, out SharedItemComponent? item);
            return item;
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public bool TryGetItem(string handName, [NotNullWhen(true)] out SharedItemComponent? item)
        {
            item = null;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return false;

            return _entities.TryGetComponent(heldEntity, out item);
        }

        /// <summary>
        ///     Tries to get the ItemComponent off the entity in the active hand.
        /// </summary>
        public SharedItemComponent? GetActiveHand
        {
            get
            {
                if (!TryGetActiveHeldEntity(out var heldEntity))
                    return null;

                _entities.TryGetComponent(heldEntity, out SharedItemComponent? item);
                return item;
            }
        }

        public IEnumerable<SharedItemComponent> GetAllHeldItems()
        {
            foreach (var entity in GetAllHeldEntities())
            {
                if (_entities.TryGetComponent(entity, out SharedItemComponent? item))
                    yield return item;
            }
        }
        #endregion
    }
}

