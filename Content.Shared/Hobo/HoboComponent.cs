using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Hobo.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HoboComponent : Component
{
}
