using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Instruments;
using NFluidsynth;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = Robust.Shared.Audio.Midi.MidiEvent;

namespace Content.Server.GameObjects.Components.Instruments
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class InstrumentComponent : SharedInstrumentComponent,
        IDropped, IHandSelected, IHandDeselected, IActivate, IUse, IThrown
    {
#pragma warning disable 649
        [Dependency] private IServerNotifyManager _notifyManager;
#pragma warning restore 649

        // These 2 values are quite high for now, and this could be easily abused. Change this if people are abusing it.
        public const int MaxMidiEventsPerSecond = 20;
        public const int MaxMidiEventsPerBatch = 50;
        public const int MaxMidiBatchDropped = 20;

        /// <summary>
        ///     The client channel currently playing the instrument, or null if there's none.
        /// </summary>
        [ViewVariables]
        private IPlayerSession _instrumentPlayer;
        private bool _handheld;

        [ViewVariables]
        private bool _playing = false;

        [ViewVariables]
        private float _timer = 0f;

        [ViewVariables]
        private int _batchesDropped = 0;

        [ViewVariables]
        private uint _lastSequencerTick = 0;

        [ViewVariables]
        private int _midiEventCount = 0;

        [ViewVariables]
        private BoundUserInterface _userInterface;

        /// <summary>
        ///     Whether the instrument is an item which can be held or not.
        /// </summary>
        [ViewVariables]
        public bool Handheld => _handheld;

        /// <summary>
        ///     Whether the instrument is currently playing or not.
        /// </summary>
        [ViewVariables]
        public bool Playing
        {
            get => _playing;
            set
            {
                _playing = value;
                Dirty();
            }
        }

        public IPlayerSession InstrumentPlayer
        {
            get => _instrumentPlayer;
            private set
            {
                Playing = false;

                if(_instrumentPlayer != null)
                    _instrumentPlayer.PlayerStatusChanged -= OnPlayerStatusChanged;

                _instrumentPlayer = value;

                if(value != null)
                    _instrumentPlayer.PlayerStatusChanged += OnPlayerStatusChanged;
            }
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
                InstrumentPlayer = null;
        }

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(InstrumentUiKey.Key);
            _userInterface.OnClosed += UserInterfaceOnClosed;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _handheld, "handheld", false);
        }

        public override ComponentState GetComponentState()
        {
            return new InstrumentState(Playing, _lastSequencerTick);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMsg:
                    if (!Playing || session != _instrumentPlayer)
                        return;

                    if (++_midiEventCount <= MaxMidiEventsPerSecond &&
                        midiEventMsg.MidiEvent.Length < MaxMidiEventsPerBatch)
                        SendNetworkMessage(midiEventMsg);
                    else
                        _batchesDropped++; // Batch dropped!

                    _lastSequencerTick = midiEventMsg.MidiEvent[^1].Timestamp;
                    break;
                case InstrumentStartMidiMessage startMidi:
                    Playing = true;
                    break;
                case InstrumentStopMidiMessage stopMidi:
                    Playing = false;
                    _lastSequencerTick = 0;
                    break;
            }
        }

        public void Dropped(DroppedEventArgs eventArgs)
        {
            Playing = false;
            SendNetworkMessage(new InstrumentStopMidiMessage());
            InstrumentPlayer = null;
            _userInterface.CloseAll();
        }

        public void Thrown(ThrownEventArgs eventArgs)
        {
            Playing = false;
            SendNetworkMessage(new InstrumentStopMidiMessage());
            InstrumentPlayer = null;
            _userInterface.CloseAll();
        }

        public void HandSelected(HandSelectedEventArgs eventArgs)
        {
            var session = eventArgs.User?.GetComponent<BasicActorComponent>()?.playerSession;

            if (session == null) return;

            InstrumentPlayer = session;
        }

        public void HandDeselected(HandDeselectedEventArgs eventArgs)
        {
            Playing = false;
            SendNetworkMessage(new InstrumentStopMidiMessage());
            _userInterface.CloseAll();
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            if (Handheld || !eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;

            if (InstrumentPlayer != null)
                return;

            InstrumentPlayer = actor.playerSession;
            OpenUserInterface(actor.playerSession);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return false;

            if(InstrumentPlayer == actor.playerSession)
                OpenUserInterface(actor.playerSession);
            return false;
        }

        private void UserInterfaceOnClosed(IPlayerSession player)
        {
            if (!Handheld && player == InstrumentPlayer)
            {
                InstrumentPlayer = null;
                SendNetworkMessage(new InstrumentStopMidiMessage());
                Playing = false;
            }
        }

        private void OpenUserInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            if (_instrumentPlayer != null && !ActionBlockerSystem.CanInteract(_instrumentPlayer.AttachedEntity))
                InstrumentPlayer = null;

            if (_batchesDropped > MaxMidiBatchDropped && InstrumentPlayer != null)
            {
                _batchesDropped = 0;
                var mob = InstrumentPlayer.AttachedEntity;

                _userInterface.CloseAll();

                if (mob.TryGetComponent(out StunnableComponent stun))
                    stun.Stun(1);
                else
                    StandingStateHelper.DropAllItemsInHands(mob);

                InstrumentPlayer = null;

                _notifyManager.PopupMessage(Owner, mob, "Your fingers cramp up from playing!");
            }

            _timer += delta;
            if (_timer < 1) return;
            _timer = 0f;
            _midiEventCount = 0;
        }
    }
}
