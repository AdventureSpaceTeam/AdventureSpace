﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

/// <summary>
/// This event is used to tell the server-inventorysystem someone wants to equip something
/// </summary>
[NetSerializable, Serializable]
public sealed class InventoryEquipActEvent : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly EntityUid ItemUid;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;

    public InventoryEquipActEvent(EntityUid uid, EntityUid itemUid, string slot, bool silent = false, bool force = false)
    {
        Uid = uid;
        ItemUid = itemUid;
        Slot = slot;
        Silent = silent;
        Force = force;
    }
}
