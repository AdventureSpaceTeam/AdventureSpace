﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Construction;
using Content.Shared.GameObjects.Components.Tag;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineFrameComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public const string PartContainer = "machine_parts";
        public const string BoardContainer = "machine_board";

        public override string Name => "MachineFrame";

        [ViewVariables]
        public bool IsComplete
        {
            get
            {
                if (!HasBoard || Requirements == null || MaterialRequirements == null)
                    return false;

                foreach (var (part, amount) in Requirements)
                {
                    if (_progress[part] < amount)
                        return false;
                }

                foreach (var (type, amount) in MaterialRequirements)
                {
                    if (_materialProgress[type] < amount)
                        return false;
                }

                foreach (var (compName, info) in ComponentRequirements)
                {
                    if (_componentProgress[compName] < info.Amount)
                        return false;
                }

                foreach (var (tagName, info) in TagRequirements)
                {
                    if (_tagProgress[tagName] < info.Amount)
                        return false;
                }

                return true;
            }
        }

        [ViewVariables]
        public bool HasBoard => _boardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        private readonly Dictionary<MachinePart, int> _progress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _materialProgress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _componentProgress = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _tagProgress = new();

        [ViewVariables]
        private Dictionary<MachinePart, int> _requirements = new();

        [ViewVariables]
        private Dictionary<string, int> _materialRequirements = new();

        [ViewVariables]
        private Dictionary<string, GenericPartInfo> _componentRequirements = new();

        [ViewVariables]
        private Dictionary<string, GenericPartInfo> _tagRequirements = new();

        [ViewVariables]
        private Container _boardContainer = default!;

        [ViewVariables]
        private Container _partContainer = default!;

        public IReadOnlyDictionary<MachinePart, int> Progress => _progress;

        public IReadOnlyDictionary<string, int> MaterialProgress => _materialProgress;

        public IReadOnlyDictionary<string, int> ComponentProgress => _componentProgress;

        public IReadOnlyDictionary<string, int> TagProgress => _tagProgress;

        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;

        public IReadOnlyDictionary<string, int> MaterialRequirements => _materialRequirements;

        public IReadOnlyDictionary<string, GenericPartInfo> ComponentRequirements => _componentRequirements;

        public IReadOnlyDictionary<string, GenericPartInfo> TagRequirements => _tagRequirements;

        public override void Initialize()
        {
            base.Initialize();

            _boardContainer = ContainerHelpers.EnsureContainer<Container>(Owner, BoardContainer);
            _partContainer = ContainerHelpers.EnsureContainer<Container>(Owner, PartContainer);
        }

        protected override void Startup()
        {
            base.Startup();

            RegenerateProgress();

            if (Owner.TryGetComponent<ConstructionComponent>(out var construction))
            {
                // Attempt to set pathfinding to the machine node...
                construction.SetNewTarget("machine");
            }
        }

        private void ResetProgressAndRequirements(MachineBoardComponent machineBoard)
        {
            _requirements = machineBoard.Requirements;
            _materialRequirements = machineBoard.MaterialIdRequirements;
            _componentRequirements = machineBoard.ComponentRequirements;
            _tagRequirements = machineBoard.TagRequirements;

            _progress.Clear();
            _materialProgress.Clear();
            _componentProgress.Clear();
            _tagProgress.Clear();

            foreach (var (machinePart, _) in Requirements)
            {
                _progress[machinePart] = 0;
            }

            foreach (var (stackType, _) in MaterialRequirements)
            {
                _materialProgress[stackType] = 0;
            }

            foreach (var (compName, _) in ComponentRequirements)
            {
                _componentProgress[compName] = 0;
            }

            foreach (var (compName, _) in TagRequirements)
            {
                _tagProgress[compName] = 0;
            }
        }

        public void RegenerateProgress()
        {
            AppearanceComponent? appearance;

            if (!HasBoard)
            {
                if (Owner.TryGetComponent(out appearance))
                {
                    appearance.SetData(MachineFrameVisuals.State, 1);
                }

                _requirements.Clear();
                _materialRequirements.Clear();
                _componentRequirements.Clear();
                _tagRequirements.Clear();
                _progress.Clear();
                _materialProgress.Clear();
                _componentProgress.Clear();
                _tagProgress.Clear();

                return;
            }

            var board = _boardContainer.ContainedEntities[0];

            if (!board.TryGetComponent<MachineBoardComponent>(out var machineBoard))
                return;

            if (Owner.TryGetComponent(out appearance))
            {
                appearance.SetData(MachineFrameVisuals.State, 2);
            }

            ResetProgressAndRequirements(machineBoard);

            foreach (var part in _partContainer.ContainedEntities)
            {
                if (part.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    // Check this is part of the requirements...
                    if (!Requirements.ContainsKey(machinePart.PartType))
                        continue;

                    if (!_progress.ContainsKey(machinePart.PartType))
                        _progress[machinePart.PartType] = 1;
                    else
                        _progress[machinePart.PartType]++;
                }

                if (part.TryGetComponent<StackComponent>(out var stack))
                {
                    var type = stack.StackTypeId;
                    // Check this is part of the requirements...
                    if (!MaterialRequirements.ContainsKey(type))
                        continue;

                    if (!_materialProgress.ContainsKey(type))
                        _materialProgress[type] = 1;
                    else
                        _materialProgress[type]++;
                }

                // I have many regrets.
                foreach (var (compName, _) in ComponentRequirements)
                {
                    var registration = _componentFactory.GetRegistration(compName);

                    if (!part.HasComponent(registration.Type))
                        continue;

                    if (!_componentProgress.ContainsKey(compName))
                        _componentProgress[compName] = 1;
                    else
                        _componentProgress[compName]++;
                }

                // I have MANY regrets.
                foreach (var (tagName, _) in TagRequirements)
                {
                    if (!part.HasTag(tagName))
                        continue;

                    if (!_tagProgress.ContainsKey(tagName))
                        _tagProgress[tagName] = 1;
                    else
                        _tagProgress[tagName]++;
                }
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!HasBoard && eventArgs.Using.TryGetComponent<MachineBoardComponent>(out var machineBoard))
            {
                if (eventArgs.Using.TryRemoveFromContainer())
                {
                    // Valid board!
                    _boardContainer.Insert(eventArgs.Using);

                    // Setup requirements and progress...
                    ResetProgressAndRequirements(machineBoard);

                    if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
                    {
                        appearance.SetData(MachineFrameVisuals.State, 2);
                    }

                    if (Owner.TryGetComponent(out ConstructionComponent? construction))
                    {
                        // So prying the components off works correctly.
                        construction.ResetEdge();
                    }

                    return true;
                }
            }
            else if (HasBoard)
            {
                if (eventArgs.Using.TryGetComponent<MachinePartComponent>(out var machinePart))
                {
                    if (!Requirements.ContainsKey(machinePart.PartType))
                        return false;

                    if (_progress[machinePart.PartType] != Requirements[machinePart.PartType]
                    && eventArgs.Using.TryRemoveFromContainer() && _partContainer.Insert(eventArgs.Using))
                    {
                        _progress[machinePart.PartType]++;
                        return true;
                    }
                }

                if (eventArgs.Using.TryGetComponent<StackComponent>(out var stack))
                {
                    var type = stack.StackTypeId;
                    if (!MaterialRequirements.ContainsKey(type))
                        return false;

                    if (_materialProgress[type] == MaterialRequirements[type])
                        return false;

                    var needed = MaterialRequirements[type] - _materialProgress[type];
                    var count = stack.Count;

                    if (count < needed)
                    {
                        if(!_partContainer.Insert(stack.Owner))
                            return false;

                        _materialProgress[type] += count;
                        return true;
                    }

                    var splitStack = new StackSplitEvent()
                        {Amount = needed, SpawnPosition = Owner.Transform.Coordinates};
                    Owner.EntityManager.EventBus.RaiseLocalEvent(stack.Owner.Uid, splitStack);

                    if (splitStack.Result == null)
                        return false;

                    if(!_partContainer.Insert(splitStack.Result))
                        return false;

                    _materialProgress[type] += needed;
                    return true;
                }

                foreach (var (compName, info) in ComponentRequirements)
                {
                    if (_componentProgress[compName] >= info.Amount)
                        continue;

                    var registration = _componentFactory.GetRegistration(compName);

                    if (!eventArgs.Using.HasComponent(registration.Type))
                        continue;

                    if (!eventArgs.Using.TryRemoveFromContainer() || !_partContainer.Insert(eventArgs.Using)) continue;
                    _componentProgress[compName]++;
                    return true;
                }

                foreach (var (tagName, info) in TagRequirements)
                {
                    if (_tagProgress[tagName] >= info.Amount)
                        continue;

                    if (!eventArgs.Using.HasTag(tagName))
                        continue;

                    if (!eventArgs.Using.TryRemoveFromContainer() || !_partContainer.Insert(eventArgs.Using)) continue;
                    _tagProgress[tagName]++;
                    return true;
                }
            }

            return false;
        }
    }
}
