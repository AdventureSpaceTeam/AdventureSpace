using System.Linq;
using Content.Shared.Radiation;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Radiation
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private const float RadiationCooldown = 0.5f;
        private float _accumulator;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulator += RadiationCooldown;

            while (_accumulator > RadiationCooldown)
            {
                _accumulator -= RadiationCooldown;

                foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>(true))
                {
                    comp.Update(frameTime);
                    var ent = comp.Owner;

                    if (ent.Deleted) continue;

                    foreach (var entity in _lookup.GetEntitiesInRange(ent.Transform.Coordinates, comp.Range))
                    {
                        // For now at least still need this because it uses a list internally then returns and this may be deleted before we get to it.
                        if (entity.Deleted) continue;

                        // Note: Radiation is liable for a refactor (stinky Sloth coding a basic version when he did StationEvents)
                        // so this ToArray doesn't really matter.
                        foreach (var radiation in entity.GetAllComponents<IRadiationAct>().ToArray())
                        {
                            radiation.RadiationAct(RadiationCooldown, comp);
                        }
                    }
                }
            }
        }
    }
}
