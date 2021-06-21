using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Checks that the entity has all parts needed in the machine frame component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class MachineFrameComplete : IGraphCondition
    {
        public async Task<bool> Condition(IEntity entity)
        {
            if (entity.Deleted || !entity.TryGetComponent<MachineFrameComponent>(out var machineFrame))
                return false;

            return machineFrame.IsComplete;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent<MachineFrameComponent>(out var machineFrame))
                return false;

            if (!machineFrame.HasBoard)
            {
                message.AddMarkup(Loc.GetString("construction-condition-machine-frame-insert-circuit-board-message"));
                return true;
            }

            if (machineFrame.IsComplete) return false;

            message.AddMarkup(Loc.GetString("construction-condition-machine-frame-requirement-label") + "\n");
            foreach (var (part, required) in machineFrame.Requirements)
            {
                var amount = required - machineFrame.Progress[part];

                if(amount == 0) continue;

                message.AddMarkup(Loc.GetString("construction-condition-machine-frame-required-element-entry",
                                                ("amount", amount),
                                                ("elementName", Loc.GetString(part.ToString())))
                                  + "\n");
            }

            foreach (var (material, required) in machineFrame.MaterialRequirements)
            {
                var amount = required - machineFrame.MaterialProgress[material];

                if(amount == 0) continue;

                message.AddMarkup(Loc.GetString("construction-condition-machine-frame-required-element-entry",
                                                ("amount", amount),
                                                ("elementName", Loc.GetString(material.ToString())))
                                  + "\n");
            }

            foreach (var (compName, info) in machineFrame.ComponentRequirements)
            {
                var amount = info.Amount - machineFrame.ComponentProgress[compName];

                if(amount == 0) continue;

                message.AddMarkup(Loc.GetString("construction-condition-machine-frame-required-element-entry",
                                                ("amount", info.Amount),
                                                ("elementName", Loc.GetString(info.ExamineName)))
                                  + "\n");
            }

            foreach (var (tagName, info) in machineFrame.TagRequirements)
            {
                var amount = info.Amount - machineFrame.TagProgress[tagName];

                if(amount == 0) continue;

                message.AddMarkup(Loc.GetString("construction-condition-machine-frame-required-element-entry",
                                                ("amount", info.Amount),
                                                ("elementName", Loc.GetString(info.ExamineName)))
                                  + "\n");
            }

            return true;
        }
    }
}
