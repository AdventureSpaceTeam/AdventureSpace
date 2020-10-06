using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Interfaces;
using Content.Shared.Network.NetMessages;
using Content.Shared.Preferences;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client
{
    /// <summary>
    ///     Receives <see cref="PlayerPreferences" /> and <see cref="GameSettings" /> from the server during the initial
    ///     connection.
    ///     Stores preferences on the server through <see cref="SelectCharacter" /> and <see cref="UpdateCharacter" />.
    /// </summary>
    public class ClientPreferencesManager : IClientPreferencesManager
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public event Action OnServerDataLoaded;
        public GameSettings Settings { get; private set; }
        public PlayerPreferences Preferences { get; private set; }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(nameof(MsgPreferencesAndSettings),
                HandlePreferencesAndSettings);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(nameof(MsgUpdateCharacter));
            _netManager.RegisterNetMessage<MsgSelectCharacter>(nameof(MsgSelectCharacter));
            _netManager.RegisterNetMessage<MsgDeleteCharacter>(nameof(MsgDeleteCharacter));
        }

        public void SelectCharacter(ICharacterProfile profile)
        {
            SelectCharacter(Preferences.IndexOfCharacter(profile));
        }

        public void SelectCharacter(int slot)
        {
            Preferences = new PlayerPreferences(Preferences.Characters, slot);
            var msg = _netManager.CreateNetMessage<MsgSelectCharacter>();
            msg.SelectedCharacterIndex = slot;
            _netManager.ClientSendMessage(msg);
        }

        public void UpdateCharacter(ICharacterProfile profile, int slot)
        {
            var characters = new Dictionary<int, ICharacterProfile>(Preferences.Characters) {[slot] = profile};
            Preferences = new PlayerPreferences(characters, Preferences.SelectedCharacterIndex);
            var msg = _netManager.CreateNetMessage<MsgUpdateCharacter>();
            msg.Profile = profile;
            msg.Slot = slot;
            _netManager.ClientSendMessage(msg);
        }

        public void CreateCharacter(ICharacterProfile profile)
        {
            var characters = new Dictionary<int, ICharacterProfile>(Preferences.Characters);
            var lowest = Enumerable.Range(0, Settings.MaxCharacterSlots)
                .Except(characters.Keys)
                .FirstOrNull();

            if (lowest == null)
            {
                throw new InvalidOperationException("Out of character slots!");
            }

            var l = lowest.Value;
            characters.Add(l, profile);
            Preferences = new PlayerPreferences(characters, Preferences.SelectedCharacterIndex);

            UpdateCharacter(profile, l);
        }

        public void DeleteCharacter(ICharacterProfile profile)
        {
            DeleteCharacter(Preferences.IndexOfCharacter(profile));
        }

        public void DeleteCharacter(int slot)
        {
            var characters = Preferences.Characters.Where(p => p.Key != slot);
            Preferences = new PlayerPreferences(characters, Preferences.SelectedCharacterIndex);
            var msg = _netManager.CreateNetMessage<MsgDeleteCharacter>();
            msg.Slot = slot;
            _netManager.ClientSendMessage(msg);
        }

        private void HandlePreferencesAndSettings(MsgPreferencesAndSettings message)
        {
            Preferences = message.Preferences;
            Settings = message.Settings;

            OnServerDataLoaded?.Invoke();
        }
    }
}
