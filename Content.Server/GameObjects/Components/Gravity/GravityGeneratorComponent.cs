using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Gravity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Gravity
{
    [RegisterComponent]
    public class GravityGeneratorComponent: SharedGravityGeneratorComponent, IAttackBy, IBreakAct, IAttackHand
    {
        private BoundUserInterface _userInterface;

        private PowerDeviceComponent _powerDevice;

        private SpriteComponent _sprite;

        private bool _switchedOn;

        private bool _intact;

        private GravityGeneratorStatus _status;

        public bool Powered => _powerDevice.Powered;

        public bool SwitchedOn => _switchedOn;

        public bool Intact => _intact;

        public GravityGeneratorStatus Status => _status;

        public bool NeedsUpdate
        {
            get
            {
                switch (_status)
                {
                    case GravityGeneratorStatus.On:
                        return !(Powered && SwitchedOn && Intact);
                    case GravityGeneratorStatus.Off:
                        return SwitchedOn || !(Powered && Intact);
                    case GravityGeneratorStatus.Unpowered:
                        return SwitchedOn || Powered || !Intact;
                    case GravityGeneratorStatus.Broken:
                        return SwitchedOn || Powered || Intact;
                    default:
                        return true; // This _should_ be unreachable
                }
            }
        }

        public override string Name => "GravityGenerator";

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(GravityGeneratorUiKey.Key);
            _userInterface.OnReceiveMessage += HandleUIMessage;
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _sprite = Owner.GetComponent<SpriteComponent>();
            _switchedOn = true;
            _intact = true;
            _status = GravityGeneratorStatus.On;
            UpdateState();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _switchedOn, "switched_on", true);
            serializer.DataField(ref _intact, "intact", true);
        }

        bool IAttackHand.AttackHand(AttackHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<IActorComponent>(out var actor))
                return false;
            if (Status != GravityGeneratorStatus.Off && Status != GravityGeneratorStatus.On)
            {
                return false;
            }
            OpenUserInterface(actor.playerSession);
            return true;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.TryGetComponent<WelderComponent>(out var welder)) return false;
            if (welder.TryUse(5.0f))
            {
                // Repair generator
                var damagable = Owner.GetComponent<DamageableComponent>();
                var breakable = Owner.GetComponent<BreakableComponent>();
                damagable.HealAllDamage();
                breakable.broken = false;
                _intact = true;

                var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
                var notifyManager = IoCManager.Resolve<IServerNotifyManager>();

                entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/welder2.ogg", Owner);
                notifyManager.PopupMessage(Owner, eventArgs.User, Loc.GetString("You repair the gravity generator with the welder"));

                return true;
            } else
            {
                return false;
            }
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _intact = false;
            _switchedOn = false;
        }

        public void UpdateState()
        {
            if (!Intact)
            {
                MakeBroken();
            } else if (!Powered)
            {
                MakeUnpowered();
            } else if (!SwitchedOn)
            {
                MakeOff();
            } else
            {
                MakeOn();
            }
        }

        private void HandleUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case GeneratorStatusRequestMessage _:
                    _userInterface.SetState(new GeneratorState(Status == GravityGeneratorStatus.On));
                    break;
                case SwitchGeneratorMessage msg:
                    _switchedOn = msg.On;
                    UpdateState();
                    break;
                default:
                    break;
            }
        }

        private void OpenUserInterface(IPlayerSession playerSession)
        {
            _userInterface.Open(playerSession);
        }

        private void MakeBroken()
        {
            _status = GravityGeneratorStatus.Broken;
            _sprite.LayerSetState(0, "broken");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeUnpowered()
        {
            _status = GravityGeneratorStatus.Unpowered;
            _sprite.LayerSetState(0, "off");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeOff()
        {
            _status = GravityGeneratorStatus.Off;
            _sprite.LayerSetState(0, "off");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeOn()
        {
            _status = GravityGeneratorStatus.On;
            _sprite.LayerSetState(0, "on");
            _sprite.LayerSetVisible(1, true);
        }
    }

    public enum GravityGeneratorStatus
    {
        Broken,
        Unpowered,
        Off,
        On
    }
}
