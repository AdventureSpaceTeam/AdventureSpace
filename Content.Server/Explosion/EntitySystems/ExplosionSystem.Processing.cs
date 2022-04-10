using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem : EntitySystem
{
    /// <summary>
    ///     Used to identify explosions when communicating with the client. Might be needed if more than one explosion is spawned in a single tick.
    /// </summary>
    /// <remarks>
    ///     Overflowing back to 0 should cause no issue, as long as you don't have more than 256 explosions happening in a single tick.
    /// </remarks>
    private int _explosionCounter = 0;
    // maybe should just use a UID/explosion-entity and a state to convey information?
    // but then need to ignore PVS? Eeehh this works well enough for now.

    /// <summary>
    ///     Used to limit explosion processing time. See <see cref="MaxProcessingTime"/>.
    /// </summary>
    internal readonly Stopwatch Stopwatch = new();

    /// <summary>
    ///     How many tiles to explode before checking the stopwatch timer
    /// </summary>
    internal static int TileCheckIteration = 1;

    /// <summary>
    ///     Queue for delayed processing of explosions. If there is an explosion that covers more than <see
    ///     cref="TilesPerTick"/> tiles, other explosions will actually be delayed slightly. Unless it's a station
    ///     nuke, this delay should never really be noticeable.
    /// </summary>
    private Queue<Func<Explosion?>> _explosionQueue = new();

    /// <summary>
    ///     The explosion currently being processed.
    /// </summary>
    private Explosion? _activeExplosion;

    /// <summary>
    ///     While processing an explosion, the "progress" is sent to clients, so that the explosion fireball effect
    ///     syncs up with the damage. When the tile iteration increments, an update needs to be sent to clients.
    ///     This integer keeps track of the last value sent to clients.
    /// </summary>
    private int _previousTileIteration;

    private void OnMapChanged(MapChangedEvent ev)
    {
        // If a map was deleted, check the explosion currently being processed belongs to that map.
        if (ev.Created)
            return;

        if (_activeExplosion?.Epicenter.MapId != ev.Map)
            return;

        _activeExplosion = null;
        _nodeGroupSystem.Snoozing = false;
    }

    /// <summary>
    ///     Process the explosion queue.
    /// </summary>
    public override void Update(float frameTime)
    {
        if (_activeExplosion == null && _explosionQueue.Count == 0)
            // nothing to do
            return;

        Stopwatch.Restart();
        var x = Stopwatch.Elapsed.TotalMilliseconds;

        var availableTime = MaxProcessingTime;

        var tilesRemaining = TilesPerTick;
        while (tilesRemaining > 0 && MaxProcessingTime > Stopwatch.Elapsed.TotalMilliseconds)
        {
            // if there is no active explosion, get a new one to process
            if (_activeExplosion == null)
            {
                // EXPLOSION TODO allow explosion spawning to be interrupted by time limit. In the meantime, ensure that
                // there is at-least 1ms of time left before creating a new explosion
                if (MathF.Max(MaxProcessingTime - 1, 0.1f)  < Stopwatch.Elapsed.TotalMilliseconds)
                    break;

                if (!_explosionQueue.TryDequeue(out var spawnNextExplosion))
                    break;

                _activeExplosion = spawnNextExplosion();

                // explosion spawning can be null if something somewhere went wrong. (e.g., negative explosion
                // intensity).
                if (_activeExplosion == null)
                    continue;

                _explosionCounter++;
                _previousTileIteration = 0;

                // just a lil nap
                if (SleepNodeSys)
                {
                    _nodeGroupSystem.Snoozing = true;
                    // snooze grid-chunk regeneration?
                    // snooze power network (recipients look for new suppliers as wires get destroyed).
                }

                if (_activeExplosion.Area > SingleTickAreaLimit)
                    break; // start processing next turn.
            }

            // TODO EXPLOSION  check if active explosion is on a paused map. If it is... I guess support swapping out &
            // storing the "currently active" explosion?

            var processed = _activeExplosion.Process(tilesRemaining);
            tilesRemaining -= processed;

            // has the explosion finished processing?
            if (_activeExplosion.FinishedProcessing)
                _activeExplosion = null;
        }

        Logger.InfoS("Explosion", $"Processed {TilesPerTick - tilesRemaining} tiles in {Stopwatch.Elapsed.TotalMilliseconds}ms");

        // we have finished processing our tiles. Is there still an ongoing explosion?
        if (_activeExplosion != null)
        {
            // update the client explosion overlays. This ensures that the fire-effects sync up with the entities currently being damaged.
            if (_previousTileIteration == _activeExplosion.CurrentIteration)
                return;

            _previousTileIteration = _activeExplosion.CurrentIteration;
            RaiseNetworkEvent(new ExplosionOverlayUpdateEvent(_explosionCounter, _previousTileIteration + 1));
            return;
        }

        if (_explosionQueue.Count > 0)
            return;

        // We have finished processing all explosions. Clear client explosion overlays
        RaiseNetworkEvent(new ExplosionOverlayUpdateEvent(_explosionCounter, int.MaxValue));

        //wakey wakey
        _nodeGroupSystem.Snoozing = false;
    }

    /// <summary>
    ///     Determines whether an entity is blocking a tile or not. (whether it can prevent the tile from being uprooted
    ///     by an explosion).
    /// </summary>
    /// <remarks>
    ///     Used for a variation of <see cref="TurfHelpers.IsBlockedTurf()"/> that makes use of the fact that we have
    ///     already done an entity lookup on a tile, and don't need to do so again.
    /// </remarks>
    public bool IsBlockingTurf(EntityUid uid, EntityQuery<PhysicsComponent> physicsQuery)
    {
        if (EntityManager.IsQueuedForDeletion(uid))
            return false;

        if (!physicsQuery.TryGetComponent(uid, out var physics))
            return false;

        return physics.CanCollide && physics.Hard && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0;
    }

    /// <summary>
    ///     Find entities on a grid tile using the EntityLookupComponent and apply explosion effects.
    /// </summary>
    /// <returns>True if the underlying tile can be uprooted, false if the tile is blocked by a dense entity</returns>
    internal bool ExplodeTile(EntityLookupComponent lookup,
        IMapGrid grid,
        Vector2i tile,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<PhysicsComponent> physicsQuery)
    {
        var gridBox = new Box2(tile * grid.TileSize, (tile + 1) * grid.TileSize);

        // get the entities on a tile. Note that we cannot process them directly, or we get
        // enumerator-changed-while-enumerating errors.
        List<(EntityUid, TransformComponent?) > list = new();

        void AddIntersecting(List<(EntityUid, TransformComponent?)> listy)
        {
            foreach (var uid in _entityLookup.GetLocalEntitiesIntersecting(lookup, ref gridBox, LookupFlags.None))
            {
                if (processed.Contains(uid))
                    continue;

                if (!xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                listy.Add((uid, xform));
            }
        }

        AddIntersecting(list);

        // process those entities
        foreach (var (entity, xform) in list)
        {
            processed.Add(entity);
            ProcessEntity(entity, epicenter, damage, throwForce, id, damageQuery, physicsQuery, xform);
        }

        // process anchored entities
        var tileBlocked = false;
        var anchoredList = grid.GetAnchoredEntities(tile).ToList();
        foreach (var entity in anchoredList)
        {
            processed.Add(entity);
            ProcessEntity(entity, epicenter, damage, throwForce, id, damageQuery, physicsQuery);
        }

        // Walls and reinforced walls will break into girders. These girders will also be considered turf-blocking for
        // the purposes of destroying floors. Again, ideally the process of damaging an entity should somehow return
        // information about the entities that were spawned as a result, but without that information we just have to
        // re-check for new anchored entities. Compared to entity spawning & deleting, this should still be relatively minor.
        if (anchoredList.Count > 0)
        {
            foreach (var entity in grid.GetAnchoredEntities(tile))
            {
                tileBlocked |= IsBlockingTurf(entity, physicsQuery);
            }
        }

        // Next, we get the intersecting entities AGAIN, but purely for throwing. This way, glass shards spawned from
        // windows will be flung outwards, and not stay where they spawned. This is however somewhat unnecessary, and a
        // prime candidate for computational cost-cutting. Alternatively, it would be nice if there was just some sort
        // of spawned-on-destruction event that could be used to automatically assemble a list of new entities that need
        // to be thrown.
        //
        // All things considered, until entity spawning & destruction is sped up, this isn't all that time consuming.
        // And throwing is disabled for nukes anyways.
        if (throwForce <= 0)
            return !tileBlocked;

        list.Clear();
        AddIntersecting(list);

        foreach (var (entity, xform) in list)
        {
            // Here we only throw, no dealing damage. Containers n such might drop their entities after being destroyed, but
            // they should handle their own damage pass-through, with their own damage reduction calculation.
            ProcessEntity(entity, epicenter, null, throwForce, id, damageQuery, physicsQuery, xform);
        }

        return !tileBlocked;
    }

    /// <summary>
    ///     Same as <see cref="ExplodeTile"/>, but for SPAAAAAAACE.
    /// </summary>
    internal void ExplodeSpace(EntityLookupComponent lookup,
        Matrix3 spaceMatrix,
        Matrix3 invSpaceMatrix,
        Vector2i tile,
        float throwForce,
        DamageSpecifier damage,
        MapCoordinates epicenter,
        HashSet<EntityUid> processed,
        string id,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<PhysicsComponent> physicsQuery)
    {
        var gridBox = new Box2(tile * DefaultTileSize, (DefaultTileSize, DefaultTileSize));
        var worldBox = spaceMatrix.TransformBox(gridBox);
        List<(EntityUid, TransformComponent)> list = new();

        void AddIntersecting(List<(EntityUid, TransformComponent)> listy)
        {
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(lookup, ref worldBox, LookupFlags.None))
            {
                if (processed.Contains(uid))
                    return;

                var xform = xformQuery.GetComponent(uid);

                if (xform.ParentUid == lookup.Owner)
                {
                    // parented directly to the map, use local position
                    if (gridBox.Contains(invSpaceMatrix.Transform(xform.LocalPosition)))
                        listy.Add((uid, xform));

                    return;
                }

                // "worldPos" should be the space/map local position.
                var worldPos = _transformSystem.GetWorldPosition(xform, xformQuery);

                // finally check if it intersects our tile
                if (gridBox.Contains(invSpaceMatrix.Transform(worldPos)))
                    listy.Add((uid, xform));
            }
        }

        AddIntersecting(list);

        foreach (var (entity, xform) in list)
        {
            processed.Add(entity);
            ProcessEntity(entity, epicenter, damage, throwForce, id, damageQuery, physicsQuery, xform);
        }

        if (throwForce <= 0)
            return;

        // Also, throw any entities that were spawned as shrapnel. Compared to entity spawning & destruction, this extra
        // lookup is relatively minor computational cost, and throwing is disabled for nukes anyways.
        list.Clear();
        AddIntersecting(list);
        foreach (var (entity, xform) in list)
        {
            ProcessEntity(entity, epicenter, null, throwForce, id, damageQuery, physicsQuery, xform);
        }
    }

    /// <summary>
    ///     This function actually applies the explosion affects to an entity.
    /// </summary>
    private void ProcessEntity(
        EntityUid uid,
        MapCoordinates epicenter,
        DamageSpecifier? damage,
        float throwForce,
        string id,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        TransformComponent? xform = null)
    {
        // damage
        if (damage != null && damageQuery.TryGetComponent(uid, out var damageable))
        {
            var ev = new GetExplosionResistanceEvent(id);
            RaiseLocalEvent(uid, ev, false);

            if (ev.Resistance == 0)
            {
                // no damage-dict multiplication required.
                _damageableSystem.TryChangeDamage(uid, damage, ignoreResistances: true, damageable: damageable);
            }
            else if (ev.Resistance < 1)
            {
                _damageableSystem.TryChangeDamage(uid, damage * (1 - ev.Resistance), ignoreResistances: true, damageable: damageable);
            }
        }

        // throw
        if (xform != null
            && !xform.Anchored
            && throwForce > 0
            && !EntityManager.IsQueuedForDeletion(uid)
            && physicsQuery.TryGetComponent(uid, out var physics)
            && physics.BodyType == BodyType.Dynamic)
        {
            // TODO purge throw helpers and pass in physics component
            _throwingSystem.TryThrow(uid, xform.WorldPosition - epicenter.Position, throwForce);
        }

        // TODO EXPLOSION puddle / flammable ignite?

        // TODO EXPLOSION deaf/ear damage? other explosion effects?
    }

    /// <summary>
    ///     Tries to damage floor tiles. Not to be confused with the function that damages entities intersecting the
    ///     grid tile.
    /// </summary>
    public void DamageFloorTile(TileRef tileRef,
        float effectiveIntensity,
        int maxTileBreak,
        bool canCreateVacuum,
        List<(Vector2i GridIndices, Tile Tile)> damagedTiles,
        ExplosionPrototype type)
    {
        if (_tileDefinitionManager[tileRef.Tile.TypeId] is not ContentTileDefinition tileDef)
            return;

        if (tileDef.IsSpace)
            canCreateVacuum = true; // is already a vacuum.

        int tileBreakages = 0;
        while (maxTileBreak > tileBreakages && _robustRandom.Prob(type.TileBreakChance(effectiveIntensity)))
        {
            tileBreakages++;
            effectiveIntensity -= type.TileBreakRerollReduction;

            // does this have a base-turf that we can break it down to?
            if (tileDef.BaseTurfs.Count == 0)
                break;

            if (_tileDefinitionManager[tileDef.BaseTurfs[^1]] is not ContentTileDefinition newDef)
                break;

            if (newDef.IsSpace && !canCreateVacuum)
                break;

            tileDef = newDef;
        }

        if (tileDef.TileId == tileRef.Tile.TypeId)
            return;

        damagedTiles.Add((tileRef.GridIndices, new Tile(tileDef.TileId)));
    }
}

/// <summary>
///     This is a data class that stores information about the area affected by an explosion, for processing by <see
///     cref="ExplosionSystem"/>.
/// </summary>
/// <remarks>
///     This is basically the output of <see cref="ExplosionSystem.GetExplosionTiles()"/>, but with some utility functions for
///     iterating over the tiles, along with the ability to keep track of what entities have already been damaged by
///     this explosion.
/// </remarks>
sealed class Explosion
{
    /// <summary>
    ///     For every grid (+ space) that the explosion reached, this data struct stores information about the tiles and
    ///     caches the entity-lookup component so that it doesn't have to be re-fetched for every tile.
    /// </summary>
    struct ExplosionData
    {
        /// <summary>
        ///     The tiles that the explosion damaged, grouped by the iteration (can be thought of as the distance from the epicenter)
        /// </summary>
        public Dictionary<int, List<Vector2i>> TileLists;

        /// <summary>
        ///     Lookup component for this grid (or space/map).
        /// </summary>
        public EntityLookupComponent Lookup;

        /// <summary>
        ///     The actual grid that this corresponds to. If null, this implies space.
        /// </summary>
        public IMapGrid? MapGrid;
    }

    private readonly List<ExplosionData> _explosionData = new();

    /// <summary>
    ///     The explosion intensity associated with each tile iteration.
    /// </summary>
    private readonly List<float> _tileSetIntensity;

    /// <summary>
    ///     Used to avoid applying explosion effects repeatedly to the same entity. Particularly important if the
    ///     explosion throws this entity, as then it will be moving while the explosion is happening.
    /// </summary>
    public readonly HashSet<EntityUid> ProcessedEntities = new();

    /// <summary>
    ///     This integer tracks how much of this explosion has been processed.
    /// </summary>
    public int CurrentIteration { get; private set; } = 0;

    /// <summary>
    ///     The prototype for this explosion. Determines tile break chance, damage, etc.
    /// </summary>
    public readonly ExplosionPrototype ExplosionType;

    /// <summary>
    ///     The center of the explosion. Used for physics throwing. Also used to identify the map on which the explosion is happening.
    /// </summary>
    public readonly MapCoordinates Epicenter;

    /// <summary>
    ///     The matrix that defines the referance frame for the explosion in space.
    /// </summary>
    private readonly Matrix3 _spaceMatrix;

    /// <summary>
    ///     Inverse of <see cref="_spaceMatrix"/>
    /// </summary>
    private readonly Matrix3 _invSpaceMatrix;

    /// <summary>
    ///     Have all the tiles on all the grids been processed?
    /// </summary>
    public bool FinishedProcessing;

    // Variables used for enumerating over tiles, grids, etc
    private DamageSpecifier _currentDamage = default!;
    private EntityLookupComponent _currentLookup = default!;
    private IMapGrid? _currentGrid;
    private float _currentIntensity;
    private float _currentThrowForce;
    private List<Vector2i>.Enumerator _currentEnumerator;
    private int _currentDataIndex;

    /// <summary>
    ///     The set of tiles that need to be updated when the explosion has finished processing. Used to avoid having
    ///     the explosion trigger chunk regeneration & shuttle-system processing every tick.
    /// </summary>
    private readonly Dictionary<IMapGrid, List<(Vector2i, Tile)>> _tileUpdateDict = new();

    // Entity Queries
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<PhysicsComponent> _physicsQuery;
    private readonly EntityQuery<DamageableComponent> _damageQuery;

    /// <summary>
    ///     Total area that the explosion covers.
    /// </summary>
    public readonly int Area;

    /// <summary>
    ///     factor used to scale the tile break chances.
    /// </summary>
    private readonly float _tileBreakScale;

    /// <summary>
    ///     Maximum number of times that an explosion will break a single tile.
    /// </summary>
    private readonly int _maxTileBreak;

    /// <summary>
    ///     Whether this explosion can turn non-vacuum tiles into vacuum-tiles.
    /// </summary>
    private readonly bool _canCreateVacuum;

    private readonly IEntityManager _entMan;
    private readonly ExplosionSystem _system;

    /// <summary>
    ///     Initialize a new instance for processing
    /// </summary>
    public Explosion(ExplosionSystem system,
        ExplosionPrototype explosionType,
        ExplosionSpaceTileFlood? spaceData,
        List<ExplosionGridTileFlood> gridData,
        List<float> tileSetIntensity,
        MapCoordinates epicenter,
        Matrix3 spaceMatrix,
        int area,
        float tileBreakScale,
        int maxTileBreak,
        bool canCreateVacuum,
        IEntityManager entMan,
        IMapManager mapMan)
    {
        _system = system;
        ExplosionType = explosionType;
        _tileSetIntensity = tileSetIntensity;
        Epicenter = epicenter;
        Area = area;

        _tileBreakScale = tileBreakScale;
        _maxTileBreak = maxTileBreak;
        _canCreateVacuum = canCreateVacuum;
        _entMan = entMan;

        _xformQuery = entMan.GetEntityQuery<TransformComponent>();
        _physicsQuery = entMan.GetEntityQuery<PhysicsComponent>();
        _damageQuery = entMan.GetEntityQuery<DamageableComponent>();

        if (spaceData != null)
        {
            var mapUid = mapMan.GetMapEntityId(epicenter.MapId);

            _explosionData.Add(new()
            {
                TileLists = spaceData.TileLists,
                Lookup = entMan.GetComponent<EntityLookupComponent>(mapUid),
                MapGrid = null
            });

            _spaceMatrix = spaceMatrix;
            _invSpaceMatrix = Matrix3.Invert(spaceMatrix);
        }

        foreach (var grid in gridData)
        {
            _explosionData.Add(new()
            {
                TileLists = grid.TileLists,
                Lookup = entMan.GetComponent<EntityLookupComponent>(grid.Grid.GridEntityId),
                MapGrid = grid.Grid
            });
        }

        if (TryGetNextTileEnumerator())
            MoveNext();
    }

    /// <summary>
    ///     Find the next tile-enumerator. This either means retrieving a set of tiles on the next grid, or incrementing
    ///     the tile iteration by one and moving back to the first grid. This will also update the current damage, current entity-lookup, etc.
    /// </summary>
    private bool TryGetNextTileEnumerator()
    {
        while (CurrentIteration < _tileSetIntensity.Count)
        {
            _currentIntensity = _tileSetIntensity[CurrentIteration];
            _currentDamage = ExplosionType.DamagePerIntensity * _currentIntensity;

            // only throw if either the explosion is small, or if this is the outer ring of a large explosion.
            var doThrow = Area < _system.ThrowLimit || CurrentIteration > _tileSetIntensity.Count - 6;
            _currentThrowForce = doThrow ? 10 * MathF.Sqrt(_currentIntensity) : 0;

            // for each grid/space tile set
            while (_currentDataIndex < _explosionData.Count)
            {
                // try get any tile hash-set corresponding to this intensity
                var tileSets = _explosionData[_currentDataIndex].TileLists;
                if (!tileSets.TryGetValue(CurrentIteration, out var tileList))
                {
                    _currentDataIndex++;
                    continue;
                }

                _currentEnumerator = tileList.GetEnumerator();
                _currentLookup = _explosionData[_currentDataIndex].Lookup;
                _currentGrid = _explosionData[_currentDataIndex].MapGrid;
                _currentDataIndex++;

                // sanity checks, in case something changed while the explosion was being processed over several ticks.
                if (_currentLookup.Deleted || _currentGrid != null && !_entMan.EntityExists(_currentGrid.GridEntityId))
                    continue;

                return true;
            }

            // All the tiles belonging to this explosion iteration have been processed. Move onto the next iteration and
            // reset the grid counter.
            CurrentIteration++;
            _currentDataIndex = 0;
        }

        // No more explosion tiles to process
        FinishedProcessing = true;
        return false;
    }

    /// <summary>
    ///     Get the next tile that needs processing
    /// </summary>
    private bool MoveNext()
    {
        if (FinishedProcessing)
            return false;

        while (!FinishedProcessing)
        {
            if (_currentEnumerator.MoveNext())
                return true;
            else
                TryGetNextTileEnumerator();
        }

        return false;
    }

    /// <summary>
    ///     Attempt to process (i.e., damage entities) some number of grid tiles.
    /// </summary>
    public int Process(int processingTarget)
    {
        // In case the explosion terminated early last tick due to exceeding the allocated processing time, use this
        // time to update the tiles.
        SetTiles();

        int processed;
        for (processed = 0; processed < processingTarget; processed++)
        {
            if (processed % ExplosionSystem.TileCheckIteration == 0 &&
                _system.Stopwatch.Elapsed.TotalMilliseconds > _system.MaxProcessingTime)
            {
                break;
            }

            // Is the current tile on a grid (instead of in space)?
            if (_currentGrid != null &&
                _currentGrid.TryGetTileRef(_currentEnumerator.Current, out var tileRef) &&
                !tileRef.Tile.IsEmpty)
            {
                if (!_tileUpdateDict.TryGetValue(_currentGrid, out var tileUpdateList))
                {
                    tileUpdateList = new();
                    _tileUpdateDict[_currentGrid] = tileUpdateList;
                }

                // damage entities on the tile. Also figures out whether there are any solid entities blocking the floor
                // from being destroyed.
                var canDamageFloor = _system.ExplodeTile(_currentLookup,
                    _currentGrid,
                    _currentEnumerator.Current,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID,
                    _xformQuery,
                    _damageQuery,
                    _physicsQuery);

                // If the floor is not blocked by some dense object, damage the floor tiles.
                if (canDamageFloor)
                    _system.DamageFloorTile(tileRef, _currentIntensity * _tileBreakScale, _maxTileBreak, _canCreateVacuum, tileUpdateList, ExplosionType);
            }
            else
            {
                // The current "tile" is in space. Damage any entities in that region
                _system.ExplodeSpace(_currentLookup,
                    _spaceMatrix,
                    _invSpaceMatrix,
                    _currentEnumerator.Current,
                    _currentThrowForce,
                    _currentDamage,
                    Epicenter,
                    ProcessedEntities,
                    ExplosionType.ID,
                    _xformQuery,
                    _damageQuery,
                    _physicsQuery);
            }

            if (!MoveNext())
                break;
        }

        // Update damaged/broken tiles on the grid.
        SetTiles();
        return processed;
    }

    private void SetTiles()
    {
        // Updating the grid can result in chunk collision regeneration & slow processing by the shuttle system.
        // Therefore, tile breaking may be configure to only happen at the end of an explosion, rather than during every
        // tick.
        if (!_system.IncrementalTileBreaking && !FinishedProcessing)
            return;

        foreach (var (grid, list) in _tileUpdateDict)
        {
            if (list.Count > 0)
            {
                grid.SetTiles(list);
            }
        }
        _tileUpdateDict.Clear();
    }
}

