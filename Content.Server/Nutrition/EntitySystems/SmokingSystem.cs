using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Forensics;
using Content.Shared.Chemistry;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using System.Linq;
using Content.Shared.Body.Components;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem : EntitySystem
    {
        [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly SharedItemSystem _items = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ForensicsSystem _forensics = default!;
        [Dependency] private readonly LungSystem _lungSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        // [Dependency] private readonly IDiseasesBridge _diseasesBridge = default!;

        private const float UpdateTimer = 3f;

        private float _timer;

        /// <summary>
        ///     We keep a list of active smokables, because iterating all existing smokables would be dumb.
        /// </summary>
        private readonly HashSet<EntityUid> _active = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<SmokableComponent, IsHotEvent>(OnSmokableIsHotEvent);
            SubscribeLocalEvent<SmokableComponent, ComponentShutdown>(OnSmokableShutdownEvent);
            SubscribeLocalEvent<SmokableComponent, GotEquippedEvent>(OnSmokeableEquipEvent);

            InitializeCigars();
            InitializePipes();
            InitializeVapes();
        }

        public void SetSmokableState(EntityUid uid, SmokableState state, SmokableComponent? smokable = null,
            AppearanceComponent? appearance = null, ClothingComponent? clothing = null)
        {
            if (!Resolve(uid, ref smokable, ref appearance, ref clothing))
                return;

            smokable.State = state;
            _appearance.SetData(uid, SmokingVisuals.Smoking, state, appearance);

            var newState = state switch
            {
                SmokableState.Lit => smokable.LitPrefix,
                SmokableState.Burnt => smokable.BurntPrefix,
                _ => smokable.UnlitPrefix
            };

            _clothing.SetEquippedPrefix(uid, newState, clothing);
            _items.SetHeldPrefix(uid, newState);

            if (state == SmokableState.Lit)
                _active.Add(uid);
            else
                _active.Remove(uid);
        }

        private void OnSmokableIsHotEvent(Entity<SmokableComponent> entity, ref IsHotEvent args)
        {
            args.IsHot = entity.Comp.State == SmokableState.Lit;
        }

        private void OnSmokableShutdownEvent(Entity<SmokableComponent> entity, ref ComponentShutdown args)
        {
            _active.Remove(entity);
        }

        private void OnSmokeableEquipEvent(Entity<SmokableComponent> entity, ref GotEquippedEvent args)
        {
            if (args.Slot == "mask")
            {
                _forensics.TransferDna(entity.Owner, args.Equipee, false);
                // _diseasesBridge.TransferDiseasesContact(entity.Owner, args.Equipee);
            }
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            // TODO Use an "active smoke" component instead, EntityQuery over that.
            foreach (var uid in _active.ToArray())
            {
                if (!TryComp(uid, out SmokableComponent? smokable))
                {
                    _active.Remove(uid);
                    continue;
                }

                if (!_solutionContainerSystem.TryGetSolution(uid, smokable.Solution, out var soln, out var solution))
                {
                    _active.Remove(uid);
                    continue;
                }

                if (smokable.ExposeTemperature > 0 && smokable.ExposeVolume > 0)
                {
                    var transform = Transform(uid);

                    if (transform.GridUid is { } gridUid)
                    {
                        var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                        _atmos.HotspotExpose(gridUid, position, smokable.ExposeTemperature, smokable.ExposeVolume, uid, true);
                    }
                }

                var inhaledSolution = _solutionContainerSystem.SplitSolution(soln.Value, smokable.InhaleAmount * _timer);

                if (solution.Volume == FixedPoint2.Zero)
                {
                    RaiseLocalEvent(uid, new SmokableSolutionEmptyEvent(), true);
                }

                if (inhaledSolution.Volume == FixedPoint2.Zero)
                    continue;

                // This is awful. I hate this so much.
                // TODO: Please, someone refactor containers and free me from this bullshit.
                if (!_container.TryGetContainingContainer(uid, out var containerManager) ||
                    !(_inventorySystem.TryGetSlotEntity(containerManager.Owner, "mask", out var inMaskSlotUid) && inMaskSlotUid == uid) ||
                    !TryComp(containerManager.Owner, out BloodstreamComponent? bloodstream))
                {
                    continue;
                }

                _reactiveSystem.DoEntityReaction(containerManager.Owner, inhaledSolution, ReactionMethod.Ingestion);
                _bloodstreamSystem.TryAddToChemicals(containerManager.Owner, inhaledSolution, bloodstream);
                if (TryComp<BodyComponent>(containerManager.Owner, out var body)) {

                    var lungs = _bodySystem.GetBodyOrganEntityComps<LungComponent>(body.Owner);
                    var numLungs = lungs.Count;

                    foreach (var lung in lungs)
                    {
                        //go through solution, check if it does any lung damage
                        foreach (var reagent in inhaledSolution.Contents)
                        {
                            var lungEv = new OnEntityInhaleToLungs();
                            RaiseLocalEvent(body.Owner, ref lungEv);

                            if (lungEv.DamageLoss > 1.0f)
                                lungEv.DamageLoss = 1.0f;

                            var amount = (float)reagent.Quantity / numLungs * (1.0f-lungEv.DamageLoss);

                            var smokeEv = new OnEntitySmoke(amount);
                            RaiseLocalEvent(body.Owner, ref smokeEv);
                        }
                    }
                }
            }

            _timer -= UpdateTimer;
        }
    }

    /// <summary>
    ///     Directed event raised when the smokable solution is empty.
    /// </summary>
    public sealed class SmokableSolutionEmptyEvent : EntityEventArgs
    {
    }

    [ByRefEvent]
    public record struct OnEntitySmoke(float Amount);
}
