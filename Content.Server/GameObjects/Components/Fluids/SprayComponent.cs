﻿using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Fluids;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    class SprayComponent : SharedSprayComponent, IAfterInteract, IUse, IActivate, IDropped
    {
        public const float SprayDistance = 3f;

        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        private ReagentUnit _transferAmount;
        private string _spraySound;
        private float _sprayVelocity;
        private float _sprayAliveTime;
        private TimeSpan _lastUseTime;
        private TimeSpan _cooldownEnd;
        private float _cooldownTime;
        private string _vaporPrototype;
        private int _vaporAmount;
        private float _vaporSpread;
        private bool _hasSafety;
        private bool _safety;

        /// <summary>
        ///     The amount of solution to be sprayer from this solution when using it
        /// </summary>
        [ViewVariables]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        /// <summary>
        ///     The speed at which the vapor starts when sprayed
        /// </summary>
        [ViewVariables]
        public float Velocity
        {
            get => _sprayVelocity;
            set => _sprayVelocity = value;
        }

        public string SpraySound => _spraySound;

        public ReagentUnit CurrentVolume => Owner.GetComponentOrNull<SolutionContainerComponent>()?.CurrentVolume ?? ReagentUnit.Zero;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out SolutionContainerComponent _);

            if (_hasSafety)
            {
                SetSafety(Owner, _safety);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _vaporPrototype, "sprayedPrototype", "Vapor");
            serializer.DataField(ref _vaporAmount, "vaporAmount", 1);
            serializer.DataField(ref _vaporSpread, "vaporSpread", 90f);
            serializer.DataField(ref _cooldownTime, "cooldownTime", 0.5f);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(10));
            serializer.DataField(ref _sprayVelocity, "sprayVelocity", 1.5f);
            serializer.DataField(ref _spraySound, "spraySound", string.Empty);
            serializer.DataField(ref _sprayAliveTime, "sprayAliveTime", 0.75f);
            serializer.DataField(ref _hasSafety, "hasSafety", false);
            serializer.DataField(ref _safety, "safety", true);
        }

        async Task IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
                return;

            if (_hasSafety && _safety)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Its safety is on!"));
                return;
            }

            if (CurrentVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It's empty!"));
                return;
            }

            var curTime = _gameTiming.CurTime;

            if(curTime < _cooldownEnd)
                return;

            var playerPos = eventArgs.User.Transform.Coordinates;
            if (eventArgs.ClickLocation.GetGridId(_serverEntityManager) != playerPos.GetGridId(_serverEntityManager))
                return;

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
            var threeQuarters = direction * 0.75f;
            var quarter = direction * 0.25f;

            var amount = Math.Max(Math.Min((contents.CurrentVolume / _transferAmount).Int(), _vaporAmount), 1);

            var spread = _vaporSpread / amount;

            for (var i = 0; i < amount; i++)
            {
                var rotation = new Angle(direction.ToAngle() + Angle.FromDegrees(spread * i) - Angle.FromDegrees(spread * (amount-1)/2));

                var (_, diffPos) = eventArgs.ClickLocation - playerPos;
                var diffNorm = diffPos.Normalized;
                var diffLength = diffPos.Length;

                var target = eventArgs.User.Transform.Coordinates.Offset((diffNorm + rotation.ToVec()).Normalized * diffLength + quarter);

                if (target.TryDistance(Owner.EntityManager, playerPos, out var distance) && distance > SprayDistance)
                    target = eventArgs.User.Transform.Coordinates.Offset(diffNorm * SprayDistance);

                var solution = contents.SplitSolution(_transferAmount);

                if (solution.TotalVolume <= ReagentUnit.Zero)
                    break;

                var vapor = _serverEntityManager.SpawnEntity(_vaporPrototype, playerPos.Offset(distance < 1 ? quarter : threeQuarters));
                vapor.Transform.LocalRotation = rotation;

                if (vapor.TryGetComponent(out AppearanceComponent appearance)) // Vapor sprite should face down.
                {
                    appearance.SetData(VaporVisuals.Rotation, -Angle.South + rotation);
                    appearance.SetData(VaporVisuals.Color, contents.SubstanceColor.WithAlpha(1f));
                    appearance.SetData(VaporVisuals.State, true);
                }

                // Add the solution to the vapor and actually send the thing
                var vaporComponent = vapor.GetComponent<VaporComponent>();
                vaporComponent.TryAddSolution(solution);

                vaporComponent.Start(rotation.ToVec(), _sprayVelocity, target, _sprayAliveTime);
            }

            //Play sound
            EntitySystem.Get<AudioSystem>().PlayFromEntity(_spraySound, Owner, AudioHelpers.WithVariation(0.125f));

            _lastUseTime = curTime;
            _cooldownEnd = _lastUseTime + TimeSpan.FromSeconds(_cooldownTime);

            if (Owner.TryGetComponent(out ItemCooldownComponent cooldown))
            {
                cooldown.CooldownStart = _lastUseTime;
                cooldown.CooldownEnd = _cooldownEnd;
            }
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
            return true;
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
        }

        private void ToggleSafety(IEntity user)
        {
            SetSafety(user, !_safety);
        }

        private void SetSafety(IEntity user, bool state)
        {
            if (!ActionBlockerSystem.CanInteract(user) || !_hasSafety)
                return;

            _safety = state;

            if(Owner.TryGetComponent(out AppearanceComponent appearance))
                appearance.SetData(SprayVisuals.Safety, _safety);
        }

        public void Dropped(DroppedEventArgs eventArgs)
        {
            if(_hasSafety && Owner.TryGetComponent(out AppearanceComponent appearance))
                appearance.SetData(SprayVisuals.Safety, _safety);
        }
    }
}
