﻿using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Content.Server.GameObjects.Components.GUI;
using Robust.Shared.Serialization;
using Robust.Shared.Log;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Robust.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.Maths;
using System;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent, IAfterInteract
    {
        [Dependency]
        private readonly ISharedNotifyManager _notifyManager;

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        public float CuffTime { get; set; }

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        public float UncuffTime { get; set; }

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        public float BreakoutTime { get; set; }

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        public float StunBonus { get; set; }

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        public bool BreakOnRemove { get; set; }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string CuffedRSI { get; set; }

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string OverlayIconState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenName { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenDesc { get; set; }

        [ViewVariables]
        public bool Broken
        {
            get
            {
                return _isBroken;
            }
            set
            {
                if (_isBroken != value)
                {
                    _isBroken = value;

                    Dirty();
                }
            }
        }

        public string StartCuffSound { get; set; }
        public string EndCuffSound { get; set; }
        public string StartBreakoutSound { get; set; }
        public string StartUncuffSound { get; set; }
        public string EndUncuffSound { get; set; }
        public Color Color { get; set; }

        // Non-exposed data fields
        private bool _isBroken = false;
        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private AudioSystem _audioSystem;
        
        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.CuffTime, "cuffTime", 5.0f);
            serializer.DataField(this, x => x.BreakoutTime, "breakoutTime", 30.0f);
            serializer.DataField(this, x => x.UncuffTime, "uncuffTime", 5.0f);
            serializer.DataField(this, x => x.StunBonus, "stunBonus", 2.0f);
            serializer.DataField(this, x => x.StartCuffSound, "startCuffSound", "/Audio/Items/Handcuffs/cuff_start.ogg");
            serializer.DataField(this, x => x.EndCuffSound, "endCuffSound", "/Audio/Items/Handcuffs/cuff_end.ogg");
            serializer.DataField(this, x => x.StartUncuffSound, "startUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");
            serializer.DataField(this, x => x.EndUncuffSound, "endUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
            serializer.DataField(this, x => x.StartBreakoutSound, "startBreakoutSound", "/Audio/Items/Handcuffs/cuff_breakout_start.ogg");
            serializer.DataField(this, x => x.CuffedRSI, "cuffedRSI", "Objects/Misc/handcuffs.rsi");
            serializer.DataField(this, x => x.OverlayIconState, "bodyIconState", "body-overlay");
            serializer.DataField(this, x => x.Color, "color", Color.White);
            serializer.DataField(this, x => x.BreakOnRemove, "breakOnRemove", false);
            serializer.DataField(this, x => x.BrokenState, "brokenIconState", string.Empty);
            serializer.DataField(this, x => x.BrokenName, "brokenName", string.Empty);
            serializer.DataField(this, x => x.BrokenDesc, "brokenDesc", string.Empty);
        }

        public override ComponentState GetComponentState()
        {
            return new HandcuffedComponentState(Broken ? BrokenState : string.Empty);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User) || !eventArgs.Target.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                return;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't cuff yourself!"));
                return;
            }

            if (Broken)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("The cuffs are broken!"));
                return;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("{0:theName} has no hands!", eventArgs.Target));
                return;
            }

            if (cuffed.CuffedHandCount == hands.Count)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("{0:theName} has no free hands to handcuff!", eventArgs.Target));
                return;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are too far away to use the cuffs!"));
                return;
            }

            _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You start cuffing {0:theName}.", eventArgs.Target));
            _notifyManager.PopupMessage(eventArgs.User, eventArgs.Target, Loc.GetString("{0:theName} starts cuffing you!", eventArgs.User));
            _audioSystem.PlayFromEntity(StartCuffSound, Owner);

            TryUpdateCuff(eventArgs.User, eventArgs.Target, cuffed); 
        }

        /// <summary>
        /// Update the cuffed state of an entity
        /// </summary>
        private async void TryUpdateCuff(IEntity user, IEntity target, CuffableComponent cuffs)
        {
            var cuffTime = CuffTime;

            if (target.TryGetComponent<StunnableComponent>(out var stun) && stun.Stunned)
            {
                cuffTime = MathF.Max(0.1f, cuffTime - StunBonus);
            }

            var doAfterEventArgs = new DoAfterEventArgs(user, cuffTime, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(EndCuffSound, Owner);
                _notifyManager.PopupMessage(user, user, Loc.GetString("You successfully cuff {0:theName}.", target));
                _notifyManager.PopupMessage(target, target, Loc.GetString("You have been cuffed by {0:theName}!", user));

                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    hands.Drop(Owner);
                    cuffs.AddNewCuffs(Owner);
                }
                else
                {
                    Logger.Warning("Unable to remove handcuffs from player's hands! This should not happen!");
                }
            }
            else
            {
                user.PopupMessage(user, Loc.GetString("You were interrupted while cuffing {0:theName}!", target));
                target.PopupMessage(target, Loc.GetString("You interrupt {0:theName} while they are cuffing you!", user));
            }
        }
    }
}
