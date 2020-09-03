﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Observer;
using Content.Shared.Body.Part;
using Content.Shared.Body.Part.Properties.Movement;
using Content.Shared.Body.Part.Properties.Other;
using Content.Shared.Body.Preset;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a collection of <see cref="IBodyPart"></see>
    ///     attached to each other.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(ISharedBodyManagerComponent))]
    [ComponentReference(typeof(IBodyManagerComponent))]
    public class BodyManagerComponent : SharedBodyManagerComponent, IBodyPartContainer, IRelayMoveInput, IBodyManagerComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IBodyNetworkFactory _bodyNetworkFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        [ViewVariables] private string _presetName = default!;

        private readonly Dictionary<string, IBodyPart> _parts = new Dictionary<string, IBodyPart>();

        [ViewVariables] private readonly Dictionary<Type, BodyNetwork> _networks = new Dictionary<Type, BodyNetwork>();

        /// <summary>
        ///     All <see cref="IBodyPart"></see> with <see cref="LegProperty"></see>
        ///     that are currently affecting move speed, mapped to how big that leg
        ///     they're on is.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<IBodyPart, float> _activeLegs = new Dictionary<IBodyPart, float>();

        [ViewVariables] public BodyTemplate Template { get; private set; } = default!;

        [ViewVariables] public BodyPreset Preset { get; private set; } = default!;

        /// <summary>
        ///     Maps <see cref="BodyTemplate"/> slot name to the <see cref="IBodyPart"/>
        ///     object filling it (if there is one).
        /// </summary>
        [ViewVariables]
        public IReadOnlyDictionary<string, IBodyPart> Parts => _parts;

        /// <summary>
        ///     List of all slots in this body, taken from the keys of
        ///     <see cref="Template"/> slots.
        /// </summary>
        public IEnumerable<string> AllSlots => Template.Slots.Keys;

        /// <summary>
        ///     List of all occupied slots in this body, taken from the values of
        ///     <see cref="Parts"/>.
        /// </summary>
        public IEnumerable<string> OccupiedSlots => Parts.Keys;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "baseTemplate",
                "bodyTemplate.Humanoid",
                template =>
                {
                    if (!_prototypeManager.TryIndex(template, out BodyTemplatePrototype templateData))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyTemplatePrototype)} found with name {template}");
                    }

                    Template = new BodyTemplate(templateData);
                },
                () => Template.Name);

            serializer.DataReadWriteFunction(
                "basePreset",
                "bodyPreset.BasicHuman",
                preset =>
                {
                    if (!_prototypeManager.TryIndex(preset, out BodyPresetPrototype presetData))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyPresetPrototype)} found with name {preset}");
                    }

                    Preset = new BodyPreset(presetData);
                },
                () => _presetName);
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadBodyPreset(Preset);
        }

        protected override void Startup()
        {
            base.Startup();

            // Just in case something activates at default health.
            ForceHealthChangedEvent();
        }

        private void LoadBodyPreset(BodyPreset preset)
        {
            _presetName = preset.Name;

            foreach (var slotName in Template.Slots.Keys)
            {
                // For each slot in our BodyManagerComponent's template,
                // try and grab what the ID of what the preset says should be inside it.
                if (!preset.PartIDs.TryGetValue(slotName, out var partId))
                {
                    // If the preset doesn't define anything for it, continue.
                    continue;
                }

                // Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                if (!_prototypeManager.TryIndex(partId, out BodyPartPrototype newPartData))
                {
                    throw new InvalidOperationException($"No {nameof(BodyPartPrototype)} prototype found with ID {partId}");
                }

                // Try and remove an existing limb if that exists.
                RemoveBodyPart(slotName, false);

                // Add a new BodyPart with the BodyPartPrototype as a baseline to our
                // BodyComponent.
                var addedPart = new BodyPart(newPartData);
                TryAddPart(slotName, addedPart);
            }

            OnBodyChanged(); // TODO: Duplicate code
        }

        /// <summary>
        ///     Changes the current <see cref="BodyTemplate"/> to the given
        ///     <see cref="BodyTemplate"/>.
        ///     Attempts to keep previous <see cref="IBodyPart"/> if there is a
        ///     slot for them in both <see cref="BodyTemplate"/>.
        /// </summary>
        public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate)
        {
            foreach (var part in Parts)
            {
                // TODO: Make this work.
            }

            OnBodyChanged();
        }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> before
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PreMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PreMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.Update(frameTime);
            }
        }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> after
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PostMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PostMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.Update(frameTime);
            }
        }

        /// <summary>
        ///     Called when the layout of this body changes.
        /// </summary>
        private void OnBodyChanged()
        {
            // Calculate move speed based on this body.
            if (Owner.HasComponent<MovementSpeedModifierComponent>())
            {
                _activeLegs.Clear();
                var legParts = Parts.Values.Where(x => x.HasProperty(typeof(LegProperty)));

                foreach (var part in legParts)
                {
                    var footDistance = DistanceToNearestFoot(this, part);

                    if (Math.Abs(footDistance - float.MinValue) > 0.001f)
                    {
                        _activeLegs.Add(part, footDistance);
                    }
                }

                CalculateSpeed();
            }
        }

        private void CalculateSpeed()
        {
            if (!Owner.TryGetComponent(out MovementSpeedModifierComponent? playerMover))
            {
                return;
            }

            float speedSum = 0;
            foreach (var part in _activeLegs.Keys)
            {
                if (!part.HasProperty<LegProperty>())
                {
                    _activeLegs.Remove(part);
                }
            }

            foreach (var (key, value) in _activeLegs)
            {
                if (key.TryGetProperty(out LegProperty? leg))
                {
                    // Speed of a leg = base speed * (1+log1024(leg length))
                    speedSum += leg.Speed * (1 + (float) Math.Log(value, 1024.0));
                }
            }

            if (speedSum <= 0.001f || _activeLegs.Count <= 0)
            {
                playerMover.BaseWalkSpeed = 0.8f;
                playerMover.BaseSprintSpeed = 2.0f;
            }
            else
            {
                // Extra legs stack diminishingly.
                // Final speed = speed sum/(leg count-log4(leg count))
                playerMover.BaseWalkSpeed =
                    speedSum / (_activeLegs.Count - (float) Math.Log(_activeLegs.Count, 4.0));

                playerMover.BaseSprintSpeed = playerMover.BaseWalkSpeed * 1.75f;
            }
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }

        #region BodyPart Functions

        /// <summary>
        ///     Recursively searches for if <see cref="target"/> is connected to
        ///     the center. Not efficient (O(n^2)), but most bodies don't have a ton
        ///     of <see cref="IBodyPart"/>s.
        /// </summary>
        /// <param name="target">The body part to find the center for.</param>
        /// <returns>True if it is connected to the center, false otherwise.</returns>
        private bool ConnectedToCenterPart(IBodyPart target)
        {
            var searchedSlots = new List<string>();

            return TryGetSlotName(target, out var result) &&
                   ConnectedToCenterPartRecursion(searchedSlots, result);
        }

        private bool ConnectedToCenterPartRecursion(ICollection<string> searchedSlots, string slotName)
        {
            if (!TryGetBodyPart(slotName, out var part))
            {
                return false;
            }

            if (part == GetCenterBodyPart())
            {
                return true;
            }

            searchedSlots.Add(slotName);

            if (!TryGetBodyPartConnections(slotName, out List<string> connections))
            {
                return false;
            }

            foreach (var connection in connections)
            {
                if (!searchedSlots.Contains(connection) &&
                    ConnectedToCenterPartRecursion(searchedSlots, connection))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Finds the central <see cref="IBodyPart"/>, if any, of this body based on
        ///     the <see cref="BodyTemplate"/>. For humans, this is the torso.
        /// </summary>
        /// <returns>The <see cref="BodyPart"/> if one exists, null otherwise.</returns>
        private IBodyPart? GetCenterBodyPart()
        {
            Parts.TryGetValue(Template.CenterSlot, out var center);
            return center;
        }

        /// <summary>
        ///     Returns whether the given slot name exists within the current
        ///     <see cref="BodyTemplate"/>.
        /// </summary>
        private bool SlotExists(string slotName)
        {
            return Template.SlotExists(slotName);
        }

        /// <summary>
        ///     Finds the <see cref="IBodyPart"/> in the given <see cref="slotName"/> if
        ///     one exists.
        /// </summary>
        /// <param name="slotName">The slot to search in.</param>
        /// <param name="result">The body part in that slot, if any.</param>
        /// <returns>True if found, false otherwise.</returns>
        private bool TryGetBodyPart(string slotName, [NotNullWhen(true)] out IBodyPart? result)
        {
            return Parts.TryGetValue(slotName, out result!);
        }

        /// <summary>
        ///     Finds the slotName that the given <see cref="IBodyPart"/> resides in.
        /// </summary>
        /// <param name="part">The <see cref="IBodyPart"/> to find the slot for.</param>
        /// <param name="result">The slot found, if any.</param>
        /// <returns>True if a slot was found, false otherwise</returns>
        private bool TryGetSlotName(IBodyPart part, [NotNullWhen(true)] out string result)
        {
            // We enforce that there is only one of each value in the dictionary,
            // so we can iterate through the dictionary values to get the key from there.
            var pair = Parts.FirstOrDefault(x => x.Value == part);
            result = pair.Key;

            return !pair.Equals(default);
        }

        /// <summary>
        ///     Finds the <see cref="BodyPartType"/> in the given
        ///     <see cref="slotName"/> if one exists.
        /// </summary>
        /// <param name="slotName">The slot to search in.</param>
        /// <param name="result">
        ///     The <see cref="BodyPartType"/> of that slot, if any.
        /// </param>
        /// <returns>True if found, false otherwise.</returns>
        public bool TryGetSlotType(string slotName, out BodyPartType result)
        {
            return Template.Slots.TryGetValue(slotName, out result);
        }

        /// <summary>
        ///     Finds the names of all slots connected to the given
        ///     <see cref="slotName"/> for the template.
        /// </summary>
        /// <param name="slotName">The slot to search in.</param>
        /// <param name="connections">The connections found, if any.</param>
        /// <returns>True if the connections are found, false otherwise.</returns>
        private bool TryGetBodyPartConnections(string slotName, [NotNullWhen(true)] out List<string> connections)
        {
            return Template.Connections.TryGetValue(slotName, out connections!);
        }

        /// <summary>
        ///     Grabs all occupied slots connected to the given slot,
        ///     regardless of whether the given <see cref="slotName"/> is occupied.
        /// </summary>
        /// <param name="slotName">The slot name to find connections from.</param>
        /// <param name="result">The connected body parts, if any.</param>
        /// <returns>
        ///     True if successful, false if there was an error or no connected
        ///     <see cref="BodyPart"/>s were found.
        /// </returns>
        public bool TryGetBodyPartConnections(string slotName, [NotNullWhen(true)] out List<IBodyPart> result)
        {
            result = null!;

            if (!Template.Connections.TryGetValue(slotName, out var connections))
            {
                return false;
            }

            var toReturn = new List<IBodyPart>();
            foreach (var connection in connections)
            {
                if (TryGetBodyPart(connection, out var bodyPartResult))
                {
                    toReturn.Add(bodyPartResult);
                }
            }

            if (toReturn.Count <= 0)
            {
                return false;
            }

            result = toReturn;
            return true;
        }

        /// <summary>
        ///     Grabs all parts connected to the given <see cref="part"/>, regardless
        ///     of whether the given <see cref="part"/> is occupied.
        /// </summary>
        /// <returns>
        ///     True if successful, false if there was an error or no connected
        ///     <see cref="IBodyPart"/>s were found.
        /// </returns>
        private bool TryGetBodyPartConnections(IBodyPart part, [NotNullWhen(true)] out List<IBodyPart> result)
        {
            result = null!;

            return TryGetSlotName(part, out var slotName) &&
                   TryGetBodyPartConnections(slotName, out result);
        }

        /// <summary>
        ///     Grabs all <see cref="IBodyPart"/> of the given type in this body.
        /// </summary>
        public List<IBodyPart> GetBodyPartsOfType(BodyPartType type)
        {
            var toReturn = new List<IBodyPart>();

            foreach (var part in Parts.Values)
            {
                if (part.PartType == type)
                {
                    toReturn.Add(part);
                }
            }

            return toReturn;
        }

        /// <summary>
        ///     Installs the given <see cref="DroppedBodyPartComponent"/> into the
        ///     given slot, deleting the <see cref="IEntity"/> afterwards.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool InstallDroppedBodyPart(DroppedBodyPartComponent part, string slotName)
        {
            DebugTools.AssertNotNull(part);

            if (!TryAddPart(slotName, part.ContainedBodyPart))
            {
                return false;
            }

            part.Owner.Delete();
            return true;
        }

        /// <summary>
        ///     Disconnects the given <see cref="IBodyPart"/> reference, potentially
        ///     dropping other <see cref="IBodyPart">BodyParts</see> if they were hanging
        ///     off of it.
        /// </summary>
        /// <returns>
        ///     The <see cref="IEntity"/> representing the dropped
        ///     <see cref="IBodyPart"/>, or null if none was dropped.
        /// </returns>
        public IEntity? DropPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            if (!_parts.ContainsValue(part))
            {
                return null;
            }

            if (!RemoveBodyPart(part, out var slotName))
            {
                return null;
            }

            // Call disconnect on all limbs that were hanging off this limb.
            if (TryGetBodyPartConnections(slotName, out List<string> connections))
            {
                // This loop is an unoptimized travesty. TODO: optimize to be less shit
                foreach (var connectionName in connections)
                {
                    if (TryGetBodyPart(connectionName, out var result) && !ConnectedToCenterPart(result))
                    {
                        DisconnectBodyPart(connectionName, true);
                    }
                }
            }

            part.SpawnDropped(out var dropped);

            OnBodyChanged();
            return dropped;
        }

        /// <summary>
        ///     Disconnects the given <see cref="IBodyPart"/> reference, potentially
        ///     dropping other <see cref="IBodyPart">BodyParts</see> if they were hanging
        ///     off of it.
        /// </summary>
        public void DisconnectBodyPart(IBodyPart part, bool dropEntity)
        {
            DebugTools.AssertNotNull(part);

            var slotName = _parts.FirstOrDefault(x => x.Value == part).Key;
            if (string.IsNullOrEmpty(slotName)) return;
            DisconnectBodyPart(slotName, dropEntity);

        }

        /// <summary>
        ///     Disconnects a body part in the given slot if one exists,
        ///     optionally dropping it.
        /// </summary>
        /// <param name="slotName">The slot to remove the body part from</param>
        /// <param name="dropEntity">
        ///     Whether or not to drop the body part as an entity if it exists.
        /// </param>
        private void DisconnectBodyPart(string slotName, bool dropEntity)
        {
            DebugTools.AssertNotNull(slotName);

            if (!HasPart(slotName))
            {
                return;
            }

            RemoveBodyPart(slotName, dropEntity);

            if (TryGetBodyPartConnections(slotName, out List<string> connections))
            {
                foreach (var connectionName in connections)
                {
                    if (TryGetBodyPart(connectionName, out var result) && !ConnectedToCenterPart(result))
                    {
                        DisconnectBodyPart(connectionName, dropEntity);
                    }
                }
            }

            OnBodyChanged();
        }

        public bool TryAddPart(string slot, IBodyPart part, bool force = false)
        {
            DebugTools.AssertNotNull(part);
            DebugTools.AssertNotNull(slot);

            // Make sure the given slot exists
            if (!force)
            {
                if (!SlotExists(slot))
                {
                    return false;
                }

                // And that nothing is in it
                if (!_parts.TryAdd(slot, part))
                {
                    return false;
                }
            }
            else
            {
                _parts[slot] = part;
            }

            part.Body = this;

            var argsAdded = new BodyPartAddedEventArgs(part, slot);

            foreach (var component in Owner.GetAllComponents<IBodyPartAdded>().ToArray())
            {
                component.BodyPartAdded(argsAdded);
            }

            // TODO: Sort this duplicate out
            OnBodyChanged();

            if (!Template.Layers.TryGetValue(slot, out var partMap) ||
                !_reflectionManager.TryParseEnumReference(partMap, out var partEnum))
            {
                Logger.Warning($"Template {Template.Name} has an invalid RSI map key {partMap} for body part {part.Name}.");
                return false;
            }

            part.RSIMap = partEnum;

            var partMessage = new BodyPartAddedMessage(part.RSIPath, part.RSIState, partEnum);

            SendNetworkMessage(partMessage);

            foreach (var mechanism in part.Mechanisms)
            {
                if (!Template.MechanismLayers.TryGetValue(mechanism.Id, out var mechanismMap))
                {
                    continue;
                }

                if (!_reflectionManager.TryParseEnumReference(mechanismMap, out var mechanismEnum))
                {
                    Logger.Warning($"Template {Template.Name} has an invalid RSI map key {mechanismMap} for mechanism {mechanism.Id}.");
                    continue;
                }

                var mechanismMessage = new MechanismSpriteAddedMessage(mechanismEnum);

                SendNetworkMessage(mechanismMessage);
            }

            return true;
        }

        public bool HasPart(string slot)
        {
            return _parts.ContainsKey(slot);
        }

        /// <summary>
        ///     Removes the body part in slot <see cref="slotName"/> from this body,
        ///     if one exists.
        /// </summary>
        /// <param name="slotName">The slot to remove it from.</param>
        /// <param name="drop">
        ///     Whether or not to drop the removed <see cref="IBodyPart"/>.
        /// </param>
        /// <returns></returns>
        private bool RemoveBodyPart(string slotName, bool drop)
        {
            DebugTools.AssertNotNull(slotName);

            if (!_parts.Remove(slotName, out var part))
            {
                return false;
            }

            IEntity? dropped = null;
            if (drop)
            {
                part.SpawnDropped(out dropped);
            }

            part.Body = null;

            var args = new BodyPartRemovedEventArgs(part, slotName);

            foreach (var component in Owner.GetAllComponents<IBodyPartRemoved>())
            {
                component.BodyPartRemoved(args);
            }

            if (part.RSIMap != null)
            {
                var message = new BodyPartRemovedMessage(part.RSIMap, dropped?.Uid);
                SendNetworkMessage(message);
            }

            foreach (var mechanism in part.Mechanisms)
            {
                if (!Template.MechanismLayers.TryGetValue(mechanism.Id, out var mechanismMap))
                {
                    continue;
                }

                if (!_reflectionManager.TryParseEnumReference(mechanismMap, out var mechanismEnum))
                {
                    Logger.Warning($"Template {Template.Name} has an invalid RSI map key {mechanismMap} for mechanism {mechanism.Id}.");
                    continue;
                }

                var mechanismMessage = new MechanismSpriteRemovedMessage(mechanismEnum);

                SendNetworkMessage(mechanismMessage);
            }

            if (CurrentDamageState == DamageState.Dead) return true;

            // creadth: fall down if no legs
            if (part.PartType == BodyPartType.Leg && Parts.Count(x => x.Value.PartType == BodyPartType.Leg) == 0)
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner);
            }

            // creadth: immediately kill entity if last vital part removed
            if (part.IsVital && Parts.Count(x => x.Value.PartType == part.PartType) == 0)
            {
                CurrentDamageState = DamageState.Dead;
                ForceHealthChangedEvent();
            }

            return true;
        }

        /// <summary>
        ///     Removes the body part from this body, if one exists.
        /// </summary>
        /// <param name="part">The part to remove from this body.</param>
        /// <param name="slotName">The slot that the part was in, if any.</param>
        /// <returns>True if <see cref="part"/> was removed, false otherwise.</returns>
        private bool RemoveBodyPart(IBodyPart part, [NotNullWhen(true)] out string? slotName)
        {
            DebugTools.AssertNotNull(part);

            var pair = _parts.FirstOrDefault(kvPair => kvPair.Value == part);

            if (pair.Equals(default))
            {
                slotName = null;
                return false;
            }

            slotName = pair.Key;

            return RemoveBodyPart(slotName, false);
        }

        #endregion

        #region BodyNetwork Functions

        private bool EnsureNetwork(BodyNetwork network)
        {
            DebugTools.AssertNotNull(network);

            if (_networks.ContainsKey(network.GetType()))
            {
                return true;
            }

            _networks.Add(network.GetType(), network);
            network.OnAdd(Owner);

            return false;
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to this body.
        /// </summary>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork(Type networkType)
        {
            DebugTools.Assert(networkType.IsSubclassOf(typeof(BodyNetwork)));

            var network = _bodyNetworkFactory.GetNetwork(networkType);
            return EnsureNetwork(network);
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to
        ///     this body.
        /// </summary>
        /// <typeparam name="T">The type of network to add.</typeparam>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork<T>() where T : BodyNetwork
        {
            return EnsureNetwork(typeof(T));
        }

        public void RemoveNetwork(Type networkType)
        {
            DebugTools.AssertNotNull(networkType);

            if (_networks.Remove(networkType, out var network))
            {
                network.OnRemove();
            }
        }

        public void RemoveNetwork<T>() where T : BodyNetwork
        {
            RemoveNetwork(typeof(T));
        }

        /// <summary>
        ///     Attempts to get the <see cref="BodyNetwork"/> of the given type in this body.
        /// </summary>
        /// <param name="networkType">The type to search for.</param>
        /// <param name="result">
        ///     The <see cref="BodyNetwork"/> if found, null otherwise.
        /// </param>
        /// <returns>True if found, false otherwise.</returns>
        public bool TryGetNetwork(Type networkType, [NotNullWhen(true)] out BodyNetwork result)
        {
            return _networks.TryGetValue(networkType, out result!);
        }

        #endregion

        #region Recursion Functions

        /// <summary>
        ///     Returns the combined length of the distance to the nearest <see cref="BodyPart"/> with a
        ///     <see cref="FootProperty"/>. Returns <see cref="float.MinValue"/>
        ///     if there is no foot found. If you consider a <see cref="BodyManagerComponent"/> a node map, then it will look for
        ///     a foot node from the given node. It can
        ///     only search through BodyParts with <see cref="ExtensionProperty"/>.
        /// </summary>
        private static float DistanceToNearestFoot(BodyManagerComponent body, IBodyPart source)
        {
            if (source.HasProperty<FootProperty>() && source.TryGetProperty<ExtensionProperty>(out var property))
            {
                return property.ReachDistance;
            }

            return LookForFootRecursion(body, source, new List<BodyPart>());
        }

        // TODO: Make this not static and not keep me up at night
        private static float LookForFootRecursion(BodyManagerComponent body, IBodyPart current,
            ICollection<BodyPart> searchedParts)
        {
            if (!current.TryGetProperty<ExtensionProperty>(out var extProperty))
            {
                return float.MinValue;
            }

            // Get all connected parts if the current part has an extension property
            if (!body.TryGetBodyPartConnections(current, out var connections))
            {
                return float.MinValue;
            }

            // If a connected BodyPart is a foot, return this BodyPart's length.
            foreach (var connection in connections)
            {
                if (!searchedParts.Contains(connection) && connection.HasProperty<FootProperty>())
                {
                    return extProperty.ReachDistance;
                }
            }

            // Otherwise, get the recursion values of all connected BodyParts and
            // store them in a list.
            var distances = new List<float>();
            foreach (var connection in connections)
            {
                if (!searchedParts.Contains(connection))
                {
                    continue;
                }

                var result = LookForFootRecursion(body, connection, searchedParts);

                if (Math.Abs(result - float.MinValue) > 0.001f)
                {
                    distances.Add(result);
                }
            }

            // If one or more of the searches found a foot, return the smallest one
            // and add this ones length.
            if (distances.Count > 0)
            {
                return distances.Min<float>() + extProperty.ReachDistance;
            }

            return float.MinValue;

            // No extension property, no go.
        }

        #endregion
    }

    public interface IBodyManagerHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyManagerHealthChangeParams : HealthChangeParams, IBodyManagerHealthChangeParams
    {
        public BodyManagerHealthChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
