﻿namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// The centerpiece for the thermo-electric generator (TEG).
/// </summary>
/// <seealso cref="TegSystem"/>
[RegisterComponent]
[Access(typeof(TegSystem))]
public sealed class TegGeneratorComponent : Component
{
    /// <summary>
    /// When transferring energy from the hot to cold side,
    /// determines how much of that energy can be extracted as electricity.
    /// </summary>
    /// <remarks>
    /// A value of 0.9 means that 90% of energy transferred goes to electricity.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("thermal_efficiency")]
    public float ThermalEfficiency = 0.65f;

    /// <summary>
    /// Simple factor that scales effective electricity generation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("power_factor")]
    public float PowerFactor = 1;

    /// <summary>
    /// Amount of energy (Joules) generated by the TEG last atmos tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("last_generation")]
    public float LastGeneration;

    /// <summary>
    /// The current target for TEG power generation.
    /// Drifts towards actual power draw of the network with <see cref="PowerFactor"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("ramp_position")]
    public float RampPosition;

    /// <summary>
    /// Factor by which TEG power generation scales, both up and down.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("ramp_factor")]
    public float RampFactor = 1.05f;

    /// <summary>
    /// Minimum position for the ramp. Avoids TEG taking too long to start.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("ramp_threshold")]
    public float RampMinimum = 5000;

    /// <summary>
    /// Power output value at which the sprite appearance and sound volume should cap out.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("max_visual_power")]
    public float MaxVisualPower = 200_000;

    /// <summary>
    /// Minimum ambient sound volume, when we're producing just barely any power at all.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("volume_min")]
    public float VolumeMin = -9;

    /// <summary>
    /// Maximum ambient sound volume, when we're producing &gt;= <see cref="MaxVisualPower"/> power.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("volume_max")]
    public float VolumeMax = -4;
}
