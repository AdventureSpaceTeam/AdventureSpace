using Content.Server.Construction.Components;
using Content.Shared.Examine;

namespace Content.Server.Construction
{
    /// <summary>
    /// Deals with machine parts and machine boards.
    /// </summary>
    public sealed class MachinePartSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MachineBoardComponent, ExaminedEvent>(OnMachineBoardExamined);
            SubscribeLocalEvent<MachinePartComponent, ExaminedEvent>(OnMachinePartExamined);
        }

        private void OnMachineBoardExamined(EntityUid uid, MachineBoardComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            args.PushMarkup(Loc.GetString("machine-board-component-on-examine-label"));
            foreach (var (part, amount) in component.Requirements)
            {
                args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", amount),
                                                ("requiredElement", Loc.GetString(part.ToString()))));
            }

            foreach (var (material, amount) in component.MaterialRequirements)
            {
                args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", amount),
                                                ("requiredElement", Loc.GetString(material.Name))));
            }

            foreach (var (_, info) in component.ComponentRequirements)
            {
                args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", info.Amount),
                                                ("requiredElement", Loc.GetString(info.ExamineName))));
            }

            foreach (var (_, info) in component.TagRequirements)
            {
                args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", info.Amount),
                                                ("requiredElement", Loc.GetString(info.ExamineName))));
            }
        }

        private void OnMachinePartExamined(EntityUid uid, MachinePartComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            args.PushMarkup(Loc.GetString("machine-part-component-on-examine-rating-text", ("rating", component.Rating)));
            args.PushMarkup(Loc.GetString("machine-part-component-on-examine-type-text", ("type", component.PartType)));
        }
    }
}
