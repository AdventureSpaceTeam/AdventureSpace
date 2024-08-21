using System.Linq;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.StationAI.UI;

[Serializable, NetSerializable]
public sealed class StationAIRequestBorgsList : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class StationAIBorgCameraRequest : BoundUserInterfaceMessage
{
    public NetEntity Borg;

    public StationAIBorgCameraRequest(NetEntity borg)
    {
        Borg = borg;
    }
}

[Serializable, NetSerializable]
public sealed class StationAIBorgUIModel
{
    public NetEntity Borg;
    public string Name;
    public string Coordinates;
    public float Percent;
    public SiliconLawset Laws;

    public StationAIBorgUIModel(NetEntity borg, string name, string coordinates, float percent, SiliconLawset laws)
    {
        Borg = borg;
        Name = name;
        Coordinates = coordinates;
        Percent = percent;
        Laws = laws;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is StationAIBorgUIModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Borg, Name, Coordinates, Percent, Laws);
    }

    private bool Equals(StationAIBorgUIModel other)
    {
        return Borg.Equals(other.Borg) && Name == other.Name && Coordinates == other.Coordinates && Percent.Equals(other.Percent) && Laws.ObeysTo == other.Laws.ObeysTo && Laws.Laws.SequenceEqual(Laws.Laws);
    }
}

[Serializable, NetSerializable]
public sealed class StationAIBorgInterfaceState : BoundUserInterfaceState
{
    public List<StationAIBorgUIModel> Borgs;

    public StationAIBorgInterfaceState(List<StationAIBorgUIModel> borgs)
    {
        Borgs = borgs;
    }
}
