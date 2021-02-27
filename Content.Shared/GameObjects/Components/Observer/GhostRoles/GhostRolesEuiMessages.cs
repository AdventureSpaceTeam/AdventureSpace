#nullable enable
using System;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer.GhostRoles
{
    [NetSerializable, Serializable]
    public struct GhostRoleInfo
    {
        public uint Identifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [NetSerializable, Serializable]
    public class GhostRolesEuiState : EuiStateBase
    {
        public GhostRoleInfo[] GhostRoles { get; }

        public GhostRolesEuiState(GhostRoleInfo[] ghostRoles)
        {
            GhostRoles = ghostRoles;
        }
    }

    [NetSerializable, Serializable]
    public class GhostRoleTakeoverRequestMessage : EuiMessageBase
    {
        public uint Identifier { get; }

        public GhostRoleTakeoverRequestMessage(uint identifier)
        {
            Identifier = identifier;
        }
    }

    [NetSerializable, Serializable]
    public class GhostRoleWindowCloseMessage : EuiMessageBase
    {
    }
}
