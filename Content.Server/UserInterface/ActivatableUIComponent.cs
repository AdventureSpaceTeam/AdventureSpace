using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;


namespace Content.Server.UserInterface
{
    [RegisterComponent]
    public sealed class ActivatableUIComponent : Component,
            ISerializationHooks
    {
        [ViewVariables]
        public Enum? Key { get; set; }

        [ViewVariables] public BoundUserInterface? UserInterface => (Key != null) ? Owner.GetUIOrNull(Key) : null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inHandsOnly")]
        public bool InHandsOnly { get; set; } = false;

        [ViewVariables]
        [DataField("singleUser")]
        public bool SingleUser { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("adminOnly")]
        public bool AdminOnly { get; set; } = false;

        [DataField("key", readOnly: true, required: true)]
        private string _keyRaw = default!;

        [DataField("verbText")]
        public string VerbText = "ui-verb-toggle-open";

        /// <summary>
        ///     The client channel currently using the object, or null if there's none/not single user.
        ///     NOTE: DO NOT DIRECTLY SET, USE ActivatableUISystem.SetCurrentSingleUser
        /// </summary>
        [ViewVariables]
        public IPlayerSession? CurrentSingleUser;

        void ISerializationHooks.AfterDeserialization()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            if (reflectionManager.TryParseEnumReference(_keyRaw, out var key))
                Key = key;
        }
    }
}

