using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests
{
    public class DummyGameTicker : SharedGameTicker, IGameTicker
    {
        public GameRunLevel RunLevel { get; } = GameRunLevel.InRound;

        public event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged
        {
            add { }
            remove { }
        }

        public event Action<GameRuleAddedEventArgs> OnRuleAdded
        {
            add{ }
            remove { }
        }

        public void Initialize()
        {
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
        }

        public void RestartRound()
        {
        }

        public void StartRound(bool force = false)
        {
        }

        public void EndRound()
        {
        }

        public void Respawn(IPlayerSession targetPlayer)
        {
        }

        public void MakeObserve(IPlayerSession player)
        {
        }

        public void MakeJoinGame(IPlayerSession player)
        {
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
        }

        public GridCoordinates GetLateJoinSpawnPoint() => GridCoordinates.InvalidGrid;
        public GridCoordinates GetJobSpawnPoint(string jobId) => GridCoordinates.InvalidGrid;
        public GridCoordinates GetObserverSpawnPoint() => GridCoordinates.InvalidGrid;

        public T AddGameRule<T>() where T : GameRule, new()
        {
            return new T();
        }

        public bool HasGameRule(Type type)
        {
            return false;
        }

        public void RemoveGameRule(GameRule rule)
        {
        }

        public IEnumerable<GameRule> ActiveGameRules { get; } = Array.Empty<GameRule>();

        public bool TryGetPreset(string name, out Type type)
        {
            type = default;
            return false;
        }

        public void SetStartPreset(Type type, bool force = false)
        {
        }

        public void SetStartPreset(string name, bool force = false)
        {
        }

        public bool DelayStart(TimeSpan time)
        {
            return true;
        }

        public bool PauseStart(bool pause = true)
        {
            return true;
        }

        public bool TogglePause()
        {
            return false;
        }
    }
}
