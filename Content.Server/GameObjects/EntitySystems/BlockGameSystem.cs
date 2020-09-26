﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Arcade;
using Content.Shared.Arcade;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BlockGameSystem : EntitySystem
    {
        private readonly List<BlockGameMessages.HighScoreEntry> _roundHighscores = new List<BlockGameMessages.HighScoreEntry>();
        private readonly List<BlockGameMessages.HighScoreEntry> _globalHighscores = new List<BlockGameMessages.HighScoreEntry>();

        public HighScorePlacement RegisterHighScore(string name, int score)
        {
            var entry = new BlockGameMessages.HighScoreEntry(name, score);
            return new HighScorePlacement(TryInsertIntoList(_roundHighscores, entry), TryInsertIntoList(_globalHighscores, entry));
        }

        public List<BlockGameMessages.HighScoreEntry> GetLocalHighscores() => GetSortedHighscores(_roundHighscores);

        public List<BlockGameMessages.HighScoreEntry> GetGlobalHighscores() => GetSortedHighscores(_globalHighscores);

        private List<BlockGameMessages.HighScoreEntry> GetSortedHighscores(List<BlockGameMessages.HighScoreEntry> highScoreEntries)
        {
            var result = highScoreEntries.ShallowClone();
            result.Sort((p1, p2) => p2.Score.CompareTo(p1.Score));
            return result;
        }

        private int? TryInsertIntoList(List<BlockGameMessages.HighScoreEntry> highScoreEntries, BlockGameMessages.HighScoreEntry entry)
        {
            if (highScoreEntries.Count < 5)
            {
                highScoreEntries.Add(entry);
                return GetPlacement(highScoreEntries, entry);
            }

            if (highScoreEntries.Min(e => e.Score) >= entry.Score) return null;

            var lowestHighscore = highScoreEntries.Min();
            highScoreEntries.Remove(lowestHighscore);
            highScoreEntries.Add(entry);
            return GetPlacement(highScoreEntries, entry);

        }

        private int? GetPlacement(List<BlockGameMessages.HighScoreEntry> highScoreEntries, BlockGameMessages.HighScoreEntry entry)
        {
            int? placement = null;
            if (highScoreEntries.Contains(entry))
            {
                highScoreEntries.Sort((p1,p2) => p2.Score.CompareTo(p1.Score));
                placement = 1 + highScoreEntries.IndexOf(entry);
            }

            return placement;
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BlockGameArcadeComponent>())
            {
                comp.DoGameTick(frameTime);
            }
        }

        public readonly struct HighScorePlacement
        {
            public readonly int? GlobalPlacement;
            public readonly int? LocalPlacement;

            public HighScorePlacement(int? globalPlacement, int? localPlacement)
            {
                GlobalPlacement = globalPlacement;
                LocalPlacement = localPlacement;
            }
        }
    }
}
