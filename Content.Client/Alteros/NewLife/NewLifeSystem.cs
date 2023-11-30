using Content.Shared.NewLife;

namespace Content.Client.NewLife
{
    public sealed class NewLifeSystem : SharedNewLifeSystem
    {
        public void OpenRespawnMenu()
        {
            var msg = new NewLifeOpenRequest();
            RaiseNetworkEvent(msg);
        }
    }
}
