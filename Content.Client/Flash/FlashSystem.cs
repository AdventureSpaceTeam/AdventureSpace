using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Flash;

public sealed class FlashSystem : SharedFlashSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private FlashOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FlashedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FlashedComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FlashedComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<FlashedComponent, StatusEffectAddedEvent>(OnStatusAdded);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, FlashedComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, FlashedComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.PercentComplete = 1.0f;
        _overlay.ScreenshotTexture = null;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, FlashedComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnShutdown(EntityUid uid, FlashedComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlay.PercentComplete = 1.0f;
            _overlay.ScreenshotTexture = null;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnStatusAdded(EntityUid uid, FlashedComponent component, StatusEffectAddedEvent args)
    {
        if (_player.LocalEntity == uid && args.Key == FlashedKey)
        {
            _overlay.ReceiveFlash();
        }
    }
}
