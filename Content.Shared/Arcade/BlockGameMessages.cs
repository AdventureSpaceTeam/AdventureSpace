﻿#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    public static class BlockGameMessages
    {
        [Serializable, NetSerializable]
        public class BlockGamePlayerActionMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGamePlayerAction PlayerAction;
            public BlockGamePlayerActionMessage(BlockGamePlayerAction playerAction)
            {
                PlayerAction = playerAction;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameVisualUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGameVisualType GameVisualType;
            public readonly BlockGameBlock[] Blocks;
            public BlockGameVisualUpdateMessage(BlockGameBlock[] blocks, BlockGameVisualType gameVisualType)
            {
                Blocks = blocks;
                GameVisualType = gameVisualType;
            }
        }

        public enum BlockGameVisualType
        {
            GameField,
            HoldBlock,
            NextBlock
        }

        [Serializable, NetSerializable]
        public class BlockGameScoreUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly int Points;
            public BlockGameScoreUpdateMessage(int points)
            {
                Points = points;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameUserStatusMessage : BoundUserInterfaceMessage
        {
            public readonly bool IsPlayer;

            public BlockGameUserStatusMessage(bool isPlayer)
            {
                IsPlayer = isPlayer;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameUserUnregisterMessage : BoundUserInterfaceMessage{}

        [Serializable, NetSerializable]
        public class BlockGameSetScreenMessage : BoundUserInterfaceMessage
        {
            public readonly BlockGameScreen Screen;
            public readonly bool isStarted;
            public BlockGameSetScreenMessage(BlockGameScreen screen, bool isStarted = true)
            {
                Screen = screen;
                this.isStarted = isStarted;
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameGameOverScreenMessage : BlockGameSetScreenMessage
        {
            public readonly int FinalScore;
            public readonly int? LocalPlacement;
            public readonly int? GlobalPlacement;
            public BlockGameGameOverScreenMessage(int finalScore, int? localPlacement, int? globalPlacement) : base(BlockGameScreen.Gameover)
            {
                FinalScore = finalScore;
                LocalPlacement = localPlacement;
                GlobalPlacement = globalPlacement;
            }
        }

        [Serializable, NetSerializable]
        public enum BlockGameScreen
        {
            Game,
            Pause,
            Gameover,
            Highscores
        }

        [Serializable, NetSerializable]
        public class BlockGameHighScoreUpdateMessage : BoundUserInterfaceMessage
        {
            public List<HighScoreEntry> LocalHighscores;
            public List<HighScoreEntry> GlobalHighscores;

            public BlockGameHighScoreUpdateMessage(List<HighScoreEntry> localHighscores, List<HighScoreEntry> globalHighscores)
            {
                LocalHighscores = localHighscores;
                GlobalHighscores = globalHighscores;
            }
        }

        [Serializable, NetSerializable]
        public class HighScoreEntry : IComparable
        {
            public string Name;
            public int Score;

            public HighScoreEntry(string name, int score)
            {
                Name = name;
                Score = score;
            }

            public int CompareTo(object? obj)
            {
                if (obj is not HighScoreEntry entry) return 0;
                return Score.CompareTo(entry.Score);
            }
        }

        [Serializable, NetSerializable]
        public class BlockGameLevelUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly int Level;
            public BlockGameLevelUpdateMessage(int level)
            {
                Level = level;
            }
        }
    }
}
