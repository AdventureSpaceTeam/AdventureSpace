﻿using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Resist;

[Serializable, NetSerializable]
public sealed class EscapeInventoryEvent : SimpleDoAfterEvent
{
}