using Content.Client.Administration.Managers;
using Robust.Client.Graphics;

namespace Content.Client.Administration
{
    public sealed partial class AdminSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private AdminNameOverlay _adminNameOverlay = default!;

        public event Action? OverlayEnabled;
        public event Action? OverlayDisabled;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(this, _entityManager, _eyeManager, _resourceCache, _entityLookup);
            _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        }

        private void ShutdownOverlay()
        {
            _adminManager.AdminStatusUpdated -= OnAdminStatusUpdated;
        }

        private void OnAdminStatusUpdated()
        {
            AdminOverlayOff();
        }

        public void AdminOverlayOn()
        {
            if (_overlayManager.HasOverlay<AdminNameOverlay>()) return;
            _overlayManager.AddOverlay(_adminNameOverlay);
            OverlayEnabled?.Invoke();
        }

        public void AdminOverlayOff()
        {
            _overlayManager.RemoveOverlay<AdminNameOverlay>();
            OverlayDisabled?.Invoke();
        }
    }
}
