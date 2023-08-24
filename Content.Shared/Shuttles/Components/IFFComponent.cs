using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Handles what a grid should look like on radar.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedShuttleSystem))]
public sealed partial class IFFComponent : Component
{
    /// <summary>
    /// Should we show IFF by default?
    /// </summary>
    public const bool ShowIFFDefault = true;

    /// <summary>
    /// Default color to use for IFF if no component is found.
    /// </summary>
    public static readonly Color IFFColor = Color.Aquamarine;

    [ViewVariables(VVAccess.ReadWrite), DataField("flags")]
    public IFFFlags Flags = IFFFlags.None;

    /// <summary>
    /// Color for this to show up on IFF.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color Color = IFFColor;
}

[Flags]
public enum IFFFlags : byte
{
    None = 0,

    /// <summary>
    /// Should the label for this grid be hidden at all ranges.
    /// </summary>
    HideLabel,

    /// <summary>
    /// Should the grid hide entirely (AKA full stealth).
    /// Will also hide the label if that is not set.
    /// </summary>
    Hide,

    // TODO: Need one that hides its outline, just replace it with a bunch of triangles or lines or something.
}
