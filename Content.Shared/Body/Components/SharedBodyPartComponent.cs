using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Components
{
    [NetworkedComponent()]
    public abstract class SharedBodyPartComponent : Component, IBodyPartContainer
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "BodyPart";

        private SharedBodyComponent? _body;

        // TODO BODY Remove
        [DataField("mechanisms")]
        private readonly List<string> _mechanismIds = new();
        public IReadOnlyList<string> MechanismIds => _mechanismIds;

        [ViewVariables]
        private readonly HashSet<SharedMechanismComponent> _mechanisms = new();

        [ViewVariables]
        public SharedBodyComponent? Body
        {
            get => _body;
            set
            {
                if (_body == value)
                {
                    return;
                }

                var old = _body;
                _body = value;

                if (old != null)
                {
                    RemovedFromBody(old);
                }

                if (value != null)
                {
                    AddedToBody(value);
                }
            }
        }

        /// <summary>
        ///     The string to show when displaying this part's name to players.
        /// </summary>
        [ViewVariables]
        public string DisplayName => Name;

        /// <summary>
        ///     <see cref="BodyPartType"/> that this <see cref="IBodyPart"/> is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        [ViewVariables]
        [DataField("partType")]
        public BodyPartType PartType { get; private set; } = BodyPartType.Other;

        /// <summary>
        ///     Determines how many mechanisms can be fit inside this
        ///     <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        [ViewVariables] [DataField("size")] public int Size { get; private set; } = 1;

        [ViewVariables] public int SizeUsed { get; private set; }

        // TODO BODY size used
        // TODO BODY surgerydata

        /// <summary>
        ///     What types of BodyParts this <see cref="SharedBodyPartComponent"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility { get; private set; } = BodyPartCompatibility.Universal;

        // TODO BODY Mechanisms occupying different parts at the body level
        [ViewVariables]
        public IReadOnlyCollection<SharedMechanismComponent> Mechanisms => _mechanisms;

        // TODO BODY Replace with a simulation of organs
        /// <summary>
        ///     Whether or not the owning <see cref="Body"/> will die if all
        ///     <see cref="SharedBodyPartComponent"/>s of this type are removed from it.
        /// </summary>
        [ViewVariables]
        [DataField("vital")]
        public bool IsVital { get; private set; } = false;

        [ViewVariables]
        [DataField("symmetry")]
        public BodyPartSymmetry Symmetry { get; private set; } = BodyPartSymmetry.None;

        [ViewVariables]
        public ISurgeryData? SurgeryDataComponent => _entMan.GetComponentOrNull<ISurgeryData>(Owner);

        protected virtual void OnAddMechanism(SharedMechanismComponent mechanism)
        {
            var prototypeId = _entMan.GetComponent<MetaDataComponent>(mechanism.Owner).EntityPrototype!.ID;

            if (!_mechanismIds.Contains(prototypeId))
            {
                _mechanismIds.Add(prototypeId);
            }

            mechanism.Part = this;
            SizeUsed += mechanism.Size;

            Dirty();
        }

        protected virtual void OnRemoveMechanism(SharedMechanismComponent mechanism)
        {
            _mechanismIds.Remove(_entMan.GetComponent<MetaDataComponent>(mechanism.Owner).EntityPrototype!.ID);
            mechanism.Part = null;
            SizeUsed -= mechanism.Size;

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            var mechanismIds = new EntityUid[_mechanisms.Count];

            var i = 0;
            foreach (var mechanism in _mechanisms)
            {
                mechanismIds[i] = mechanism.Owner;
                i++;
            }

            return new BodyPartComponentState(mechanismIds);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not BodyPartComponentState state)
            {
                return;
            }

            var newMechanisms = state.Mechanisms();

            foreach (var mechanism in _mechanisms.ToArray())
            {
                if (!newMechanisms.Contains(mechanism))
                {
                    RemoveMechanism(mechanism);
                }
            }

            foreach (var mechanism in newMechanisms)
            {
                if (!_mechanisms.Contains(mechanism))
                {
                    TryAddMechanism(mechanism, true);
                }
            }
        }

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryDataComponent?.CheckSurgery(surgery) ?? false;
        }

        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, EntityUid performer)
        {
            DebugTools.AssertNotNull(toolType);
            DebugTools.AssertNotNull(target);
            DebugTools.AssertNotNull(surgeon);
            DebugTools.AssertNotNull(performer);

            return SurgeryDataComponent?.PerformSurgery(toolType, target, surgeon, performer) ?? false;
        }

        public bool CanAttachPart(SharedBodyPartComponent part)
        {
            DebugTools.AssertNotNull(part);

            return SurgeryDataComponent?.CanAttachBodyPart(part) ?? false;
        }

        public virtual bool CanAddMechanism(SharedMechanismComponent mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            return SurgeryDataComponent != null &&
                   SizeUsed + mechanism.Size <= Size &&
                   SurgeryDataComponent.CanAddMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to add a <see cref="SharedMechanismComponent"/> to this part.
        /// </summary>
        /// <param name="mechanism">The mechanism to add.</param>
        /// <param name="force">
        ///     Whether or not to check if the mechanism is compatible.
        ///     Passing true does not guarantee it to be added, for example if
        ///     it was already added before.
        /// </param>
        /// <returns>true if added, false otherwise even if it was already added.</returns>
        public bool TryAddMechanism(SharedMechanismComponent mechanism, bool force = false)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!force && !CanAddMechanism(mechanism))
            {
                return false;
            }

            if (!_mechanisms.Add(mechanism))
            {
                return false;
            }

            OnAddMechanism(mechanism);

            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this part.
        /// </summary>
        /// <param name="mechanism">The mechanism to remove.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        public bool RemoveMechanism(SharedMechanismComponent mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            OnRemoveMechanism(mechanism);

            return true;
        }

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this
        ///     part and drops it at the specified coordinates.
        /// </summary>
        /// <param name="mechanism">The mechanism to remove.</param>
        /// <param name="coordinates">The coordinates to drop it at.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        public bool RemoveMechanism(SharedMechanismComponent mechanism, EntityCoordinates coordinates)
        {
            if (RemoveMechanism(mechanism))
            {
                _entMan.GetComponent<TransformComponent>(mechanism.Owner).Coordinates = coordinates;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to destroy the given <see cref="SharedMechanismComponent"/> from
        ///     this part.
        ///     The mechanism won't be deleted if it is not in this body part.
        /// </summary>
        /// <returns>
        ///     True if the mechanism was in this body part and destroyed,
        ///     false otherwise.
        /// </returns>
        public bool DeleteMechanism(SharedMechanismComponent mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!RemoveMechanism(mechanism))
            {
                return false;
            }

            _entMan.DeleteEntity(mechanism.Owner);
            return true;
        }

        private void AddedToBody(SharedBodyComponent body)
        {
            _entMan.GetComponent<TransformComponent>(Owner).LocalRotation = 0;
            _entMan.GetComponent<TransformComponent>(Owner).AttachParent(body.Owner);
            OnAddedToBody(body);

            foreach (var mechanism in _mechanisms)
            {
                _entMan.EventBus.RaiseLocalEvent(mechanism.Owner, new AddedToBodyEvent(body));
            }
        }

        private void RemovedFromBody(SharedBodyComponent old)
        {
            if (!_entMan.GetComponent<TransformComponent>(Owner).Deleted)
            {
                _entMan.GetComponent<TransformComponent>(Owner).AttachToGridOrMap();
            }

            OnRemovedFromBody(old);

            foreach (var mechanism in _mechanisms)
            {
                _entMan.EventBus.RaiseLocalEvent(mechanism.Owner, new RemovedFromBodyEvent(old));
            }
        }

        protected virtual void OnAddedToBody(SharedBodyComponent body) { }

        protected virtual void OnRemovedFromBody(SharedBodyComponent old) { }

        /// <summary>
        ///     Gibs the body part.
        /// </summary>
        public virtual void Gib()
        {
            foreach (var mechanism in _mechanisms)
            {
                RemoveMechanism(mechanism);
            }
        }
    }

    [Serializable, NetSerializable]
    public class BodyPartComponentState : ComponentState
    {
        [NonSerialized] private List<SharedMechanismComponent>? _mechanisms;

        public readonly EntityUid[] MechanismIds;

        public BodyPartComponentState(EntityUid[] mechanismIds)
        {
            MechanismIds = mechanismIds;
        }

        public List<SharedMechanismComponent> Mechanisms(IEntityManager? entityManager = null)
        {
            if (_mechanisms != null)
            {
                return _mechanisms;
            }

            IoCManager.Resolve(ref entityManager);

            var mechanisms = new List<SharedMechanismComponent>(MechanismIds.Length);

            foreach (var id in MechanismIds)
            {
                if (!entityManager.EntityExists(id))
                {
                    continue;
                }

                if (!entityManager.TryGetComponent(id, out SharedMechanismComponent? mechanism))
                {
                    continue;
                }

                mechanisms.Add(mechanism);
            }

            return _mechanisms = mechanisms;
        }
    }
}
