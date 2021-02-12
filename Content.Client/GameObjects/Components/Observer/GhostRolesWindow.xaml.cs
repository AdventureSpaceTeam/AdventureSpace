using System;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Observer
{
    [GenerateTypedNameReferences]
    public partial class GhostRolesWindow : SS14Window
    {
        public event Action<uint> RoleRequested;

        protected override Vector2 CalculateMinimumSize() => (350, 275);

        public void ClearEntries()
        {
            EntryContainer.DisposeAllChildren();
            NoRolesMessage.Visible = true;
        }

        public void AddEntry(GhostRoleInfo info)
        {
            NoRolesMessage.Visible = false;
            EntryContainer.AddChild(new GhostRolesEntry(info, _ => RoleRequested?.Invoke(info.Identifier)));
        }
    }
}
