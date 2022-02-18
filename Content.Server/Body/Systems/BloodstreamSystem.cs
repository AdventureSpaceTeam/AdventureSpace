﻿using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SpillableSystem _spillableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    // TODO here
    // Update over time. Modify bloodloss damage in accordance with (amount of blood / max blood level), and reduce bleeding over time
    // Sub to damage changed event and modify bloodloss if incurring large hits of slashing/piercing

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var bloodstream in EntityManager.EntityQuery<BloodstreamComponent>())
        {
            bloodstream.AccumulatedFrametime += frameTime;

            if (bloodstream.AccumulatedFrametime < bloodstream.UpdateInterval)
                continue;

            bloodstream.AccumulatedFrametime -= bloodstream.UpdateInterval;

            var uid = bloodstream.Owner;
            if (TryComp<MobStateComponent>(uid, out var state) && state.IsDead())
                continue;

            // First, let's refresh their blood if possible.
            if (bloodstream.BloodSolution.CurrentVolume < bloodstream.BloodSolution.MaxVolume)
                TryModifyBloodLevel(uid, bloodstream.BloodRefreshAmount, bloodstream);

            // Next, let's remove some blood from them according to their bleed level.
            // as well as stop their bleeding to a certain extent.
            if (bloodstream.BleedAmount > 0)
            {
                TryModifyBloodLevel(uid, (-bloodstream.BleedAmount) / 10, bloodstream);
                TryModifyBleedAmount(uid, -bloodstream.BleedReductionAmount, bloodstream);
            }

            // Next, we'll deal some bloodloss damage if their blood level is below a threshold.
            var bloodPercentage = GetBloodLevelPercentage(uid, bloodstream);
            if (bloodPercentage < bloodstream.BloodlossThreshold)
            {
                // TODO use a better method for determining this.
                var amt = bloodstream.BloodlossDamage / bloodPercentage;

                _damageableSystem.TryChangeDamage(uid, amt, true, false);
            }
            else
            {
                // If they're healthy, we'll try and heal some bloodloss instead.
                _damageableSystem.TryChangeDamage(uid, bloodstream.BloodlossHealDamage * bloodPercentage, true, false);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, BloodstreamComponent component, ComponentInit args)
    {
        component.ChemicalSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultChemicalsSolutionName);
        component.BloodSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultBloodSolutionName);
        component.BloodTemporarySolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultBloodTemporarySolutionName);

        component.ChemicalSolution.MaxVolume = component.ChemicalMaxVolume;
        component.BloodSolution.MaxVolume = component.BloodMaxVolume;
        component.BloodTemporarySolution.MaxVolume = component.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well

        // Fill blood solution with BLOOD
        _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent,
            component.BloodMaxVolume, out _);
    }

    private void OnDamageChanged(EntityUid uid, BloodstreamComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        // TODO probably cache this or something. humans get hurt a lot
        if (!_prototypeManager.TryIndex<DamageModifierSetPrototype>(component.DamageBleedModifiers, out var modifiers))
            return;

        var bloodloss = DamageSpecifier.ApplyModifierSet(args.DamageDelta, modifiers);

        if (bloodloss.Empty)
            return;

        var total = bloodloss.Total;
        var totalFloat = total.Float();
        TryModifyBleedAmount(uid, totalFloat, component);

        var prob = Math.Clamp(totalFloat / 50, 0, 1);
        if (_robustRandom.Prob(prob))
        {
            // This is gonna hurt.
            TryModifyBloodLevel(uid, (-total) / 5, component);
            SoundSystem.Play(Filter.Pvs(uid), component.InstantBloodSound.GetSound(), uid, AudioParams.Default);
        }
    }

    private void OnExamined(EntityUid uid, BloodstreamComponent component, ExaminedEvent args)
    {
        if (GetBloodLevelPercentage(uid, component) < component.BloodlossThreshold)
        {
            args.PushMarkup(Loc.GetString("bloodstream-component-looks-pale", ("target", uid)));
        }
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToChemicals(EntityUid uid, Solution solution, BloodstreamComponent? component=null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return _solutionContainerSystem.TryAddSolution(uid, component.ChemicalSolution, solution);
    }

    public float GetBloodLevelPercentage(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0.0f;

        return (component.BloodSolution.CurrentVolume / component.BloodSolution.MaxVolume).Float();
    }

    /// <summary>
    ///     Attempts to modify the blood level of this entity directly.
    /// </summary>
    public bool TryModifyBloodLevel(EntityUid uid, FixedPoint2 amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (amount >= 0)
            return _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent, amount, out _);

        // Removal is more involved,
        // since we also wanna handle moving it to the temporary solution
        // and then spilling it if necessary.
        var newSol = component.BloodSolution.SplitSolution(-amount);
        component.BloodTemporarySolution.AddSolution(newSol);

        if (component.BloodTemporarySolution.CurrentVolume > component.BleedPuddleThreshold)
        {
            // Pass some of the chemstream into the spilled blood.
            var temp = component.ChemicalSolution.SplitSolution(component.BloodTemporarySolution.CurrentVolume / 10);
            component.BloodTemporarySolution.AddSolution(temp);
            _spillableSystem.SpillAt(uid, component.BloodTemporarySolution, "PuddleBlood", false);
            component.BloodTemporarySolution.RemoveAllSolution();
        }

        return true;
    }

    /// <summary>
    ///     Tries to make an entity bleed more or less
    /// </summary>
    public bool TryModifyBleedAmount(EntityUid uid, float amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        component.BleedAmount += amount;
        component.BleedAmount = Math.Clamp(component.BleedAmount, 0, 40);

        return true;
    }
}
