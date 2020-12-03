﻿using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachinePartComponent : Component, IExamine
    {
        // I'm so sorry for hard-coding this. But trust me, it should make things less painful.
        public static IReadOnlyDictionary<MachinePart, string> Prototypes { get; } = new Dictionary<MachinePart, string>()
        {
            {MachinePart.Capacitor, "CapacitorStockPart"},
            {MachinePart.ScanningModule, "ScanningModuleStockPart"},
            {MachinePart.Manipulator, "MicroManipulatorStockPart"},
            {MachinePart.Laser, "MicroLaserStockPart"},
            {MachinePart.MatterBin, "MatterBinStockPart"},
            {MachinePart.Ansible, "AnsibleSubspaceStockPart"},
            {MachinePart.Filter, "FilterSubspaceStockPart"},
            {MachinePart.Amplifier, "AmplifierSubspaceStockPart"},
            {MachinePart.Treatment, "TreatmentSubspaceStockPart"},
            {MachinePart.Analyzer, "AnalyzerSubspaceStockPart"},
            {MachinePart.Crystal, "CrystalSubspaceStockPart"},
            {MachinePart.Transmitter, "TransmitterSubspaceStockPart"}
        };

        public override string Name => "MachinePart";

        [ViewVariables]
        public MachinePart PartType { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Rating { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.PartType, "part", MachinePart.Capacitor);
            serializer.DataField(this, x => x.Rating, "rating", 1);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("[color=white]Rating:[/color] [color=cyan]{0}[/color]\n", Rating));
            message.AddMarkup(Loc.GetString("[color=white]Type:[/color] [color=cyan]{0}[/color]\n", PartType));
        }
    }
}
