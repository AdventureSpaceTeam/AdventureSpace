#nullable enable
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.BarSign
{
    [Prototype("barSign")]
    public class BarSignPrototype : IPrototype
    {
        private string _description = string.Empty;
        private string _name = string.Empty;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;


        [DataField("icon")] public string Icon { get; private set; } = string.Empty;

        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        [DataField("description")]
        public string Description
        {
            get => _description;
            private set => _description = Loc.GetString(value);
        }

        [DataField("renameArea")]
        public bool RenameArea { get; private set; } = true;
        [DataField("hidden")]
        public bool Hidden { get; private set; }
    }
}
