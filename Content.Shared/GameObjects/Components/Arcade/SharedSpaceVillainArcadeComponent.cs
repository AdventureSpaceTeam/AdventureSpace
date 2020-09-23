﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Arcade
{
    public class SharedSpaceVillainArcadeComponent : Component
    {
        public override string Name => "SpaceVillainArcade";
        public override uint? NetID => ContentNetIDs.SPACE_VILLAIN_ARCADE;

        [Serializable, NetSerializable]
        public enum PlayerAction
        {
            Attack,
            Heal,
            Recharge,
            NewGame,
            RequestData
        }

        [Serializable, NetSerializable]
        public enum SpaceVillainArcadeVisualState
        {
            Normal,
            Off,
            Broken,
            Win,
            GameOver,
        }

        [Serializable, NetSerializable]
        public enum SpaceVillainArcadeUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class SpaceVillainArcadePlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly PlayerAction PlayerAction;
            public SpaceVillainArcadePlayerActionMessage(PlayerAction playerAction)
            {
                PlayerAction = playerAction;
            }
        }

        [Serializable, NetSerializable]
        public class SpaceVillainArcadeMetaDataUpdateMessage : SpaceVillainArcadeDataUpdateMessage
        {
            public readonly string GameTitle;
            public readonly string EnemyName;
            public SpaceVillainArcadeMetaDataUpdateMessage(int playerHp, int playerMp, int enemyHp, int enemyMp, string playerActionMessage, string enemyActionMessage, string gameTitle, string enemyName) : base(playerHp, playerMp, enemyHp, enemyMp, playerActionMessage, enemyActionMessage)
            {
                GameTitle = gameTitle;
                EnemyName = enemyName;
            }
        }

        [Serializable, NetSerializable]
        public class SpaceVillainArcadeDataUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly int PlayerHP;
            public readonly int PlayerMP;
            public readonly int EnemyHP;
            public readonly int EnemyMP;
            public readonly string PlayerActionMessage;
            public readonly string EnemyActionMessage;
            public SpaceVillainArcadeDataUpdateMessage(int playerHp, int playerMp, int enemyHp, int enemyMp, string playerActionMessage, string enemyActionMessage)
            {
                PlayerHP = playerHp;
                PlayerMP = playerMp;
                EnemyHP = enemyHp;
                EnemyMP = enemyMp;
                EnemyActionMessage = enemyActionMessage;
                PlayerActionMessage = playerActionMessage;
            }
        }
    }
}
