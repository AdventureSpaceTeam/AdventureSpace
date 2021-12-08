using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Server.UserInterface;
using Content.Shared.Configurable;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Configurable
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedConfigurationComponent))]
    public class ConfigurationComponent : SharedConfigurationComponent, IInteractUsing, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ConfigurationUiKey.Key);

        [DataField("keys")] private List<string> _keys = new();

        [ViewVariables]
        private readonly Dictionary<string, string> _config = new();

        [DataField("validation")]
        private readonly Regex _validation = new ("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _qualityNeeded = "Pulsing";

        void ISerializationHooks.BeforeSerialization()
        {
            _keys = _config.Keys.ToList();
        }

        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var key in _keys)
            {
                _config.Add(key, string.Empty);
            }
        }

        protected override void OnAdd()
        {
            base.OnAdd();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
            }
        }

        public string? GetConfig(string name)
        {
            return _config.GetValueOrDefault(name);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateUserInterface();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (UserInterface == null || !_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return false;

            if (!_entMan.TryGetComponent<ToolComponent?>(eventArgs.Using, out var tool) || !tool.Qualities.Contains(_qualityNeeded))
                return false;

            OpenUserInterface(actor);
            return true;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            var config = new Dictionary<string, string>(_config);

            if (message is ConfigurationUpdatedMessage msg)
            {
                foreach (var key in config.Keys)
                {
                    var value = msg.Config.GetValueOrDefault(key);

                    if (value == null || _validation != null && !_validation.IsMatch(value) && value != string.Empty)
                        continue;

                    _config[key] = value;
                }

#pragma warning disable 618
                SendMessage(new ConfigUpdatedComponentMessage(config));
#pragma warning restore 618
            }
        }

        private void UpdateUserInterface()
        {
            UserInterface?.SetState(new ConfigurationBoundUserInterfaceState(_config));
        }

        public void OpenUserInterface(ActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
            UserInterface?.SendMessage(new ValidationUpdateMessage(_validation.ToString()), actor.PlayerSession);
        }

        private static void FillConfiguration<T>(List<string> list, Dictionary<string, T> configuration, T value){
            for (var index = 0; index < list.Count; index++)
            {
                configuration.Add(list[index], value);
            }
        }
    }
}
