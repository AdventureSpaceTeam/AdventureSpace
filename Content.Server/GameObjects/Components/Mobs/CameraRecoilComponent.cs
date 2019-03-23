using Content.Shared.GameObjects.Components.Mobs;
using SS14.Shared.Maths;

namespace Content.Server.GameObjects.Components.Mobs
{
    public sealed class CameraRecoilComponent : SharedCameraRecoilComponent
    {
        public override void Kick(Vector2 recoil)
        {
            var msg = new RecoilKickMessage(recoil);
            SendNetworkMessage(msg);
        }
    }
}
