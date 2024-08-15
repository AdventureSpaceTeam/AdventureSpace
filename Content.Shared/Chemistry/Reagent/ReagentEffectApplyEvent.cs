using Content.Shared.EntityEffects;

namespace Content.Shared.Chemistry.Reagent;

[ByRefEvent]
public record struct ReagentEffectApplyEvent(EntityEffectReagentArgs Args);
