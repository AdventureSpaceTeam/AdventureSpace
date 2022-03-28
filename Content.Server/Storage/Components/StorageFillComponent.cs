using System.Collections.Generic;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Storage.Components
{
    [RegisterComponent, Friend(typeof(StorageSystem))]
    public sealed class StorageFillComponent : Component
    {
        [DataField("contents")] public List<EntitySpawnEntry> Contents = new();
    }
}
