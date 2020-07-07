using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent]
    public class PuddleComponent : Component
    {
        // Current design: Something calls the SpillHelper.Spill, that will either
        // A) Add to an existing puddle at the location (normalised to tile-center) or
        // B) add a new one
        // From this every time a puddle is spilt on it will try and overflow to its neighbours if possible,
        // and also update its appearance based on volume level (opacity) and chemistry color
        // Small puddles will evaporate after a set delay

        // TODO: 'leaves fluidtracks', probably in a separate component for stuff like gibb chunks?;
        // TODO: Add stuff like slipping -> probably in a separate component (for stuff like bananas)

        // based on behaviour (e.g. someone being punched vs slashed with a sword would have different blood sprite)
        // to check for low volumes for evaporation or whatever

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "Puddle";

        private CancellationTokenSource _evaporationToken;
        private ReagentUnit _evaporateThreshold; // How few <Solution Quantity> we can hold prior to self-destructing
        private float _evaporateTime;
        private string _spillSound;

        /// <summary>
        /// Whether or not this puddle is currently overflowing onto its neighbors
        /// </summary>
        private bool _overflown;

        private SpriteComponent _spriteComponent;
        private SnapGridComponent _snapGrid;

        public ReagentUnit MaxVolume
        {
            get => _contents.MaxVolume;
            set => _contents.MaxVolume = value;
        }

        [ViewVariables]
        public ReagentUnit CurrentVolume => _contents.CurrentVolume;

        // Volume at which the fluid will try to spill to adjacent components
        // Currently a random number, potentially change
        public ReagentUnit OverflowVolume => _overflowVolume;
        [ViewVariables]
        private ReagentUnit _overflowVolume;
        private ReagentUnit OverflowLeft => CurrentVolume - OverflowVolume;

        private SolutionComponent _contents;
        private int _spriteVariants;
        // Whether the underlying solution color should be used
        private bool _recolor;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _spillSound, "spill_sound", "/Audio/Effects/Fluids/splat.ogg");
            serializer.DataField(ref _overflowVolume, "overflow_volume", ReagentUnit.New(20));
            serializer.DataField(ref _evaporateTime, "evaporate_time", 600.0f);
            // Long-term probably have this based on the underlying reagents
            serializer.DataField(ref _evaporateThreshold, "evaporate_threshold", ReagentUnit.New(2));
            serializer.DataField(ref _spriteVariants, "variants", 1);
            serializer.DataField(ref _recolor, "recolor", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out SolutionComponent solutionComponent))
            {
                _contents = solutionComponent;
            }
            else
            {
                _contents = Owner.AddComponent<SolutionComponent>();
                _contents.Initialize();
            }

            _snapGrid = Owner.GetComponent<SnapGridComponent>();

            // Smaller than 1m^3 for now but realistically this shouldn't be hit
            MaxVolume = ReagentUnit.New(1000);

            // Random sprite state set server-side so it's consistent across all clients
            _spriteComponent = Owner.GetComponent<SpriteComponent>();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var randomVariant = robustRandom.Next(0, _spriteVariants - 1);
            var baseName = new ResourcePath(_spriteComponent.BaseRSIPath).FilenameWithoutExtension;

            _spriteComponent.LayerSetState(0, $"{baseName}-{randomVariant}"); // TODO: Remove hardcode
            _spriteComponent.Rotation = Angle.FromDegrees(robustRandom.Next(0, 359));
            // UpdateAppearance should get called soon after this so shouldn't need to call Dirty() here
        }

        // Flow rate should probably be controlled globally so this is it for now
        internal bool TryAddSolution(Solution solution, bool sound = true, bool checkForEvaporate = true, bool checkForOverflow = true)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }
            var result = _contents.TryAddSolution(solution);
            if (!result)
            {
                return false;
            }

            UpdateStatus();

            if (checkForOverflow)
            {
                CheckOverflow();
            }

            if (checkForEvaporate)
            {
                CheckEvaporate();
            }

            UpdateAppearance();
            if (!sound)
            {
                return true;
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords(_spillSound, Owner.Transform.GridPosition);
            return true;
        }

        internal Solution SplitSolution(ReagentUnit quantity)
        {
            var split = _contents.SplitSolution(quantity);
            CheckEvaporate();
            UpdateAppearance();
            return split;
        }

        public void CheckEvaporate()
        {
            if (CurrentVolume == 0)
            {
                Owner.Delete();
            }
        }

        private void UpdateStatus()
        {
            // If UpdateStatus is getting called again it means more fluid has been updated so let's just wait
            _evaporationToken?.Cancel();

            if (CurrentVolume > _evaporateThreshold)
            {
                return;
            }

            _evaporationToken = new CancellationTokenSource();

            // KYS to evaporate
            Timer.Spawn(TimeSpan.FromSeconds(_evaporateTime), CheckEvaporate, _evaporationToken.Token);
        }

        private void UpdateAppearance()
        {
            if (Owner.Deleted)
            {
                return;
            }
            // Opacity based on level of fullness to overflow
            // Hard-cap lower bound for visibility reasons
            var volumeScale = (CurrentVolume.Float() / OverflowVolume.Float()) * 0.75f + 0.25f;
            var cappedScale = Math.Min(1.0f, volumeScale);
            // Color based on the underlying solutioncomponent
            Color newColor;
            if (_recolor)
            {
                newColor = _contents.SubstanceColor.WithAlpha(cappedScale);
            }
            else
            {
                newColor = _spriteComponent.Color.WithAlpha(cappedScale);
            }

            _spriteComponent.Color = newColor;

            _spriteComponent.Dirty();
        }

        /// <summary>
        /// Will overflow this entity to neighboring entities if required
        /// </summary>
        private void CheckOverflow()
        {
            if (CurrentVolume <= _overflowVolume || _overflown)
            {
                return;
            }

            var nextPuddles = new List<PuddleComponent>() {this};
            var overflownPuddles = new List<PuddleComponent>();

            while (OverflowLeft > ReagentUnit.Zero && nextPuddles.Count > 0)
            {
                foreach (var next in nextPuddles.ToArray())
                {
                    nextPuddles.Remove(next);

                    next._overflown = true;
                    overflownPuddles.Add(next);

                    var adjacentPuddles = next.GetAllAdjacentOverflow().ToArray();
                    if (OverflowLeft <= ReagentUnit.Epsilon * adjacentPuddles.Length)
                    {
                        break;
                    }

                    if (adjacentPuddles.Length == 0)
                    {
                        continue;
                    }

                    var numberOfAdjacent = ReagentUnit.New(adjacentPuddles.Length);
                    var overflowSplit = OverflowLeft / numberOfAdjacent;
                    foreach (var adjacent in adjacentPuddles)
                    {
                        var adjacentPuddle = adjacent();
                        var quantity = ReagentUnit.Min(overflowSplit, adjacentPuddle.OverflowVolume);
                        var spillAmount = _contents.SplitSolution(quantity);

                        adjacentPuddle.TryAddSolution(spillAmount, false, false, false);
                        nextPuddles.Add(adjacentPuddle);
                    }
                }
            }

            foreach (var puddle in overflownPuddles)
            {
                puddle._overflown = false;
            }
        }

        // TODO: Move the below to SnapGrid?
        /// <summary>
        /// Will yield a random direction until none are left
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<Direction> RandomDirections()
        {
            var directions = new[]
            {
                Direction.East,
                Direction.SouthEast,
                Direction.South,
                Direction.SouthWest,
                Direction.West,
                Direction.NorthWest,
                Direction.North,
                Direction.NorthEast,
            };

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var n = directions.Length;

            while (n > 1)
            {
                n--;
                var k = robustRandom.Next(n + 1);
                var value = directions[k];
                directions[k] = directions[n];
                directions[n] = value;
            }

            foreach (var direction in directions)
            {
                yield return direction;
            }
        }

        /// <summary>
        /// Tries to get an adjacent coordinate to overflow to, unless it is blocked by a wall on the
        /// same tile or the tile is empty
        /// </summary>
        /// <param name="direction">The direction to get the puddle from, respective to this one</param>
        /// <param name="puddle">The puddle that was found or is to be created, or null if there
        /// is a wall in the way</param>
        /// <returns>true if a puddle was found or created, false otherwise</returns>
        private bool TryGetAdjacentOverflow(Direction direction, out Func<PuddleComponent> puddle)
        {
            puddle = default;

            var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);

            // If space return early, let that spill go out into the void
            var tileRef = mapGrid.GetTileRef(Owner.Transform.GridPosition.Offset(direction.ToVec()));
            if (tileRef.Tile.IsEmpty)
            {
                return false;
            }

            foreach (var entity in _snapGrid.GetInDir(direction))
            {
                if (entity.TryGetComponent(out CollidableComponent collidable) &&
                    (collidable.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    puddle = default;
                    return false;
                }

                if (entity.TryGetComponent(out PuddleComponent existingPuddle))
                {
                    if (existingPuddle._overflown)
                    {
                        return false;
                    }

                    puddle = () => existingPuddle;
                }
            }

            if (puddle == default)
            {
                var grid = _snapGrid.DirectionToGrid(direction);
                var entityManager = IoCManager.Resolve<IEntityManager>();
                puddle = () => entityManager.SpawnEntity(Owner.Prototype.ID, grid).GetComponent<PuddleComponent>();
            }

            return true;
        }

        /// <summary>
        /// Finds or creates adjacent puddles in random directions from this one
        /// </summary>
        /// <returns>Enumerable of the puddles found or to be created</returns>
        private IEnumerable<Func<PuddleComponent>> GetAllAdjacentOverflow()
        {
            foreach (var direction in RandomDirections())
            {
                if (TryGetAdjacentOverflow(direction, out var puddle))
                {
                    yield return puddle;
                }
            }
        }
    }
}
