﻿namespace Content.Shared.Administration.Logs;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum LogType
{
    Unknown = 0, // do not use
    // DamageChange = 1
    Damaged = 2,
    Healed = 3,
    Slip = 4,
    EventAnnounced = 5,
    EventStarted = 6,
    EventRan = 16,
    EventStopped = 7,
    ShuttleCalled = 8,
    ShuttleRecalled = 9,
}
