﻿using Content.Server.GameTicking;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Station;

/// <summary>
///     Added to grids saved in maps to designate that they are the 'main station' grid.
/// </summary>
[RegisterComponent, ComponentProtoName("BecomesStation")]
[Friend(typeof(GameTicker))]
public class BecomesStationComponent : Component
{
    /// <summary>
    ///     Mapping only. Should use StationIds in all other
    ///     scenarios.
    /// </summary>
    [DataField("id", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Id = default!;
}
