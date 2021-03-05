#nullable enable
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class TileNotBlocked : IConstructionCondition
    {
        [DataField("filterMobs")] private bool _filterMobs = false;
        [DataField("failIfSpace")] private bool _failIfSpace = true;

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var tileRef = location.GetTileRef();

            if (tileRef == null || tileRef.Value.IsSpace())
                return !_failIfSpace;

            return !tileRef.Value.IsBlockedTurf(_filterMobs);
        }
    }
}
