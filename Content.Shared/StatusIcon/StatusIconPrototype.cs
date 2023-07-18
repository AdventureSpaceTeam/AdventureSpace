using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.StatusIcon;

/// <summary>
/// A data structure that holds relevant
/// information for status icons.
/// </summary>
[Virtual, DataDefinition]
public class StatusIconData : IComparable<StatusIconData>
{
    /// <summary>
    /// The icon that's displayed on the entity.
    /// </summary>
    [DataField("icon", required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// A priority for the order in which the icons will be displayed.
    /// </summary>
    [DataField("priority")]
    public int Priority = 10;

    /// <summary>
    /// A preference for where the icon will be displayed. None | Left | Right
    /// </summary>
    [DataField("locationPreference")]
    public StatusIconLocationPreference LocationPreference = StatusIconLocationPreference.None;

    public int CompareTo(StatusIconData? other)
    {
        return Priority.CompareTo(other?.Priority ?? int.MaxValue);
    }
}

/// <summary>
/// <see cref="StatusIconData"/> but in new convenient prototype form!
/// </summary>
[Prototype("statusIcon")]
public sealed class StatusIconPrototype : StatusIconData, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}

[Serializable, NetSerializable]
public enum StatusIconLocationPreference : byte
{
    None,
    Left,
    Right,
}
