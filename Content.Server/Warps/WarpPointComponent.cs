using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Warps
{
    [RegisterComponent]
    public sealed class WarpPointComponent : Component, IExamine
    {
        public override string Name => "WarpPoint";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("location")] public string? Location { get; set; }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var loc = Location == null ? "<null>" : $"'{Location}'";
            message.AddText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
        }
    }
}
