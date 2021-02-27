﻿#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedResearchClientComponent : Component
    {
        public override string Name => "ResearchClient";

        /// <summary>
        ///     Request that the server updates the client.
        /// </summary>
        [Serializable, NetSerializable]
        public class ResearchClientSyncMessage : BoundUserInterfaceMessage
        {

            public ResearchClientSyncMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the server when the client chooses a research server.
        /// </summary>
        [Serializable, NetSerializable]
        public class ResearchClientServerSelectedMessage : BoundUserInterfaceMessage
        {
            public int ServerId;

            public ResearchClientServerSelectedMessage(int serverId)
            {
                ServerId = serverId;
            }
        }

        /// <summary>
        ///     Sent to the server when the client deselects a research server.
        /// </summary>
        [Serializable, NetSerializable]
        public class ResearchClientServerDeselectedMessage : BoundUserInterfaceMessage
        {
            public ResearchClientServerDeselectedMessage()
            {
            }
        }

        [NetSerializable, Serializable]
        public enum ResearchClientUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public sealed class ResearchClientBoundInterfaceState : BoundUserInterfaceState
        {
            public int ServerCount;
            public string[] ServerNames;
            public int[] ServerIds;
            public int SelectedServerId;

            public ResearchClientBoundInterfaceState(int serverCount, string[] serverNames, int[] serverIds, int selectedServerId = -1)
            {
                ServerCount = serverCount;
                ServerNames = serverNames;
                ServerIds = serverIds;
                SelectedServerId = selectedServerId;
            }
        }
    }

}
