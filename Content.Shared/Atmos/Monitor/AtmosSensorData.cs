using Content.Shared.Atmos.Monitor.Components;

namespace Content.Shared.Atmos.Monitor;

public sealed class AtmosSensorData : IAtmosDeviceData
{
    public AtmosSensorData(float pressure, float temperature, float totalMoles, AtmosMonitorAlarmType alarmState, Dictionary<Gas, float> gases, bool onFire, AtmosAlarmThreshold pressureThreshold, AtmosAlarmThreshold temperatureThreshold, Dictionary<Gas, AtmosAlarmThreshold> gasThresholds)
    {
        Pressure = pressure;
        Temperature = temperature;
        TotalMoles = totalMoles;
        AlarmState = alarmState;
        Gases = gases;
        OnFire = onFire;
        PressureThreshold = pressureThreshold;
        TemperatureThreshold = temperatureThreshold;
        GasThresholds = gasThresholds;
    }

    public bool Enabled { get; set; }
    public bool Dirty { get; set; }
    public bool IgnoreAlarms { get; set; }

    /// Most fields are readonly, because it's data that's meant to be transmitted.

    /// <summary>
    ///     Current pressure detected by this sensor.
    /// </summary>
    public float Pressure { get; }
    /// <summary>
    ///     Current temperature detected by this sensor.
    /// </summary>
    public float Temperature { get; }
    /// <summary>
    ///     Current amount of moles detected by this sensor.
    /// </summary>
    public float TotalMoles { get; }
    /// <summary>
    ///     Current alarm state of this sensor. Does not reflect the highest alarm state on the network.
    /// </summary>
    public AtmosMonitorAlarmType AlarmState { get; }
    /// <summary>
    ///     Current number of gases on this sensor.
    /// </summary>
    public Dictionary<Gas, float> Gases { get; }
    /// <summary>
    ///     If this sensor is currently detecting a fire.
    /// </summary>
    public bool OnFire { get; }

    public AtmosAlarmThreshold PressureThreshold { get; }
    public AtmosAlarmThreshold TemperatureThreshold { get; }
    public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; }
}
