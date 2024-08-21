using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.StationAI.UI;


[Serializable, NetSerializable]
public sealed class StationAIRequestCameraList : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class StationAIRequestBackToBody : BoundUserInterfaceMessage
{

}


[Serializable, NetSerializable]
public sealed class StationAISelectedCamera : BoundUserInterfaceMessage
{
    public NetEntity Camera;

    public StationAISelectedCamera(NetEntity camera)
    {
        Camera = camera;
    }
}

[Serializable, NetSerializable]
public sealed class StationAICamerasInterfaceState : BoundUserInterfaceState
{
    public List<StationAICameraUIModel> Cameras;

    public StationAICamerasInterfaceState(List<StationAICameraUIModel> cameras)
    {
        Cameras = cameras;
    }
}

[Serializable]
[NetSerializable]
public sealed class StationAICameraUIModel
{
    public NetEntity Camera;
    public NetCoordinates Coordinates;
    public bool Available;

    public StationAICameraUIModel(NetEntity camera, NetCoordinates coordinates, bool available)
    {
        Camera = camera;
        Coordinates = coordinates;
        Available = available;
    }
}
