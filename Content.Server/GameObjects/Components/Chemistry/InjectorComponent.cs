﻿using System;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Server behavior for reagent injectors and syringes. Can optionally support both
    /// injection and drawing or just injection. Can inject/draw reagents from solution
    /// containers, and can directly inject into a mobs bloodstream.
    /// </summary>
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IAfterAttack, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        /// <summary>
        /// Whether or not the injector is able to draw from containers or if it's a single use
        /// device that can only inject.
        /// </summary>
        [ViewVariables]
        private bool _injectOnly;

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only, it will
        /// attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables]
        private ReagentUnit _transferAmount;

        /// <summary>
        /// Initial storage volume of the injector
        /// </summary>
        [ViewVariables]
        private ReagentUnit _initialMaxVolume;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
        /// only ever be set to Inject
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private InjectorToggleMode _toggleState;
        /// <summary>
        /// Internal solution container
        /// </summary>
        [ViewVariables]
        private SolutionComponent _internalContents;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _injectOnly, "injectOnly", false);
            serializer.DataField(ref _initialMaxVolume, "initialMaxVolume", ReagentUnit.New(15));
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
        }
        protected override void Startup()
        {
            base.Startup();
            _internalContents = Owner.GetComponent<SolutionComponent>();
            _internalContents.Capabilities |= SolutionCaps.Injector;
            //Set _toggleState based on prototype
            _toggleState = _injectOnly ? InjectorToggleMode.Inject : InjectorToggleMode.Draw;
        }

        /// <summary>
        /// Toggle between draw/inject state if applicable
        /// </summary>
        private void Toggle(IEntity user)
        {
            if (_injectOnly)
            {
                return;
            }

            string msg;
            switch (_toggleState)
            {
                case InjectorToggleMode.Inject:
                    _toggleState = InjectorToggleMode.Draw;
                    msg = "Now drawing";
                    break;
                case InjectorToggleMode.Draw:
                    _toggleState = InjectorToggleMode.Inject;
                    msg = "Now injecting";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _notifyManager.PopupMessage(Owner, user, Loc.GetString(msg));

            Dirty();
        }

        /// <summary>
        /// Called when clicking on entities while holding in active hand
        /// </summary>
        /// <param name="eventArgs"></param>
        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            //Make sure we have the attacking entity
            if (eventArgs.Attacked == null || !_internalContents.Injector)
            {
                return;
            }

            var targetEntity = eventArgs.Attacked;
            //Handle injecting/drawing for solutions
            if (targetEntity.TryGetComponent<SolutionComponent>(out var targetSolution) && targetSolution.Injectable)
            {
                if (_toggleState == InjectorToggleMode.Inject)
                {
                    TryInject(targetSolution, eventArgs.User);
                }
                else if (_toggleState == InjectorToggleMode.Draw)
                {
                    TryDraw(targetSolution, eventArgs.User);
                }
            }
            else //Handle injecting into bloodstream
            {
                if (targetEntity.TryGetComponent<BloodstreamComponent>(out var bloodstream) && _toggleState == InjectorToggleMode.Inject)
                {
                    TryInjectIntoBloodstream(bloodstream, eventArgs.User);
                }
            }
        }

        /// <summary>
        /// Called when use key is pressed when held in active hand
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
            return true;
        }

        private void TryInjectIntoBloodstream(BloodstreamComponent targetBloodstream, IEntity user)
        {
            if (_internalContents.CurrentVolume == 0)
            {
                return;
            }

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetBloodstream.EmptyVolume);
            if (realTransferAmount <= 0)
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    Loc.GetString("Container full."));
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = _internalContents.SplitSolution(realTransferAmount);
            if (!targetBloodstream.TryTransferSolution(removedSolution))
            {
                return;
            }

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                Loc.GetString("Injected {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        private void TryInject(SolutionComponent targetSolution, IEntity user)
        {
            if (_internalContents.CurrentVolume == 0)
            {
                return;
            }

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.EmptyVolume);
            if (realTransferAmount <= 0)
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    Loc.GetString("Container full."));
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = _internalContents.SplitSolution(realTransferAmount);
            if (!targetSolution.TryAddSolution(removedSolution))
            {
                return;
            }

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                Loc.GetString("Injected {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        private void TryDraw(SolutionComponent targetSolution, IEntity user)
        {
            if (_internalContents.EmptyVolume == 0)
            {
                return;
            }

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(_transferAmount, targetSolution.CurrentVolume);
            if (realTransferAmount <= 0)
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    Loc.GetString("Container empty"));
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = targetSolution.SplitSolution(realTransferAmount);
            if (!_internalContents.TryAddSolution(removedSolution))
            {
                return;
            }

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                Loc.GetString("Drew {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new InjectorComponentState(_internalContents.CurrentVolume, _internalContents.MaxVolume, _toggleState);
        }
    }
}
