﻿using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class DrinkSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
            SubscribeLocalEvent<DrinkComponent, LandEvent>(HandleLand);
        }

        private void HandleLand(EntityUid uid, DrinkComponent component, LandEvent args)
        {
            if (component.Pressurized &&
                !component.Opened &&
                _random.Prob(0.25f) &&
                _solutionContainerSystem.TryGetDrainableSolution(uid, out var interactions))
            {
                component.Opened = true;

                var entity = EntityManager.GetEntity(uid);

                var solution = _solutionContainerSystem.Drain(uid, interactions, interactions.DrainAvailable);
                solution.SpillAt(entity, "PuddleSmear");

                SoundSystem.Play(Filter.Pvs(entity), component.BurstSound.GetSound(), entity, AudioParams.Default.WithVolume(-4));
            }
        }

        private void OnDrinkInit(EntityUid uid, DrinkComponent component, ComponentInit args)
        {
            component.Opened = component.DefaultToOpened;

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetComponent(out DrainableSolutionComponent? existingDrainable))
            {
                // Beakers have Drink component but they should use the existing Drainable
                component.SolutionName = existingDrainable.Solution;
            }
            else
            {
                _solutionContainerSystem.EnsureSolution(owner, component.SolutionName);
            }

            component.UpdateAppearance();
        }


        private void OnSolutionChange(EntityUid uid, DrinkComponent component, SolutionChangedEvent args)
        {
            component.UpdateAppearance();
        }
    }
}
