using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageWeatherMod")]
public sealed class SalvageWeatherMod : IPrototype, IBiomeSpecificMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    /// <inheritdoc/>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    /// <inheritdoc/>
    [DataField("biomes", customTypeSerializer: typeof(PrototypeIdListSerializer<SalvageBiomeMod>))]
    public List<string>? Biomes { get; } = null;

    /// <summary>
    /// Weather prototype to use on the planet.
    /// </summary>
    [DataField("weather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
    public string WeatherPrototype = string.Empty;
}
