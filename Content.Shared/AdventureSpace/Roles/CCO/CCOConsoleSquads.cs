using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.CCO;

[Serializable] [NetSerializable]
public sealed class CcoConsoleSpecialSquadModel(List<CcoConsoleSpecialSquad> squads, bool wasCalled)
{
    public List<CcoConsoleSpecialSquad> Squads = squads;
    public bool WasCalled = wasCalled;
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleSpecialSquad(string id, string name, string description)
{
    public string Description = description;
    public string Id = id;
    public string Name = name;
}
