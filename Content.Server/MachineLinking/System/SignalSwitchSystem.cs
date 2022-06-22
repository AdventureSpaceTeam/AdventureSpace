using Content.Server.MachineLinking.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalSwitchSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalSwitchComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalSwitchComponent, ActivateInWorldEvent>(OnActivated);
        }

        private void OnInit(EntityUid uid, SignalSwitchComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.OnPort, component.OffPort);
        }

        private void OnActivated(EntityUid uid, SignalSwitchComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            component.State = !component.State;
            _signalSystem.InvokePort(uid, component.State ? component.OnPort : component.OffPort);
            SoundSystem.Play(component.ClickSound.GetSound(), Filter.Pvs(component.Owner), component.Owner,
                AudioHelpers.WithVariation(0.125f).WithVolume(8f));

            args.Handled = true;
        }
    }
}
