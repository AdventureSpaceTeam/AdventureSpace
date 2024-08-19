using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Buildings.Altar.Rituals;

[Serializable, NetSerializable]
public record NarsiRitualUIModel(string PrototypeId, string Name, string Description, string Requirements, bool Available);
