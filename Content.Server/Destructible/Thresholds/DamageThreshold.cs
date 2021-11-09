using System.Collections.Generic;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Destructible.Thresholds
{
    [DataDefinition]
    public class DamageThreshold
    {
        [DataField("behaviors")]
        private List<IThresholdBehavior> _behaviors = new();

        /// <summary>
        ///     Whether or not this threshold was triggered in the previous call to
        ///     <see cref="Reached"/>.
        /// </summary>
        [ViewVariables] public bool OldTriggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [ViewVariables]
        [DataField("triggered")]
        public bool Triggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [ViewVariables]
        [DataField("triggersOnce")]
        public bool TriggersOnce { get; set; }

        /// <summary>
        ///     The trigger that decides if this threshold has been reached.
        /// </summary>
        [ViewVariables]
        [DataField("trigger")]
        public IThresholdTrigger? Trigger { get; set; }

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public IReadOnlyList<IThresholdBehavior> Behaviors => _behaviors;

        public bool Reached(DamageableComponent damageable, DestructibleSystem system)
        {
            if (Trigger == null)
            {
                return false;
            }

            if (Triggered && TriggersOnce)
            {
                return false;
            }

            if (OldTriggered)
            {
                OldTriggered = Trigger.Reached(damageable, system);
                return false;
            }

            if (!Trigger.Reached(damageable, system))
            {
                return false;
            }

            OldTriggered = true;
            return true;
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to get dependency and
        ///     system references from, if relevant.
        /// </param>
        public void Execute(EntityUid owner, DestructibleSystem system, IEntityManager entityManager)
        {
            Triggered = true;

            foreach (var behavior in Behaviors)
            {
                // The owner has been deleted. We stop execution of behaviors here.
                if (!entityManager.EntityExists(owner))
                    return;

                behavior.Execute(owner, system);
            }
        }
    }
}
