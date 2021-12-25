﻿using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Events;

/// <summary>
///     Raised at the start of <see cref="GameTicker.StartRound"/>, after round id has been incremented
/// </summary>
public class RoundStartingEvent : EntityEventArgs
{
}
