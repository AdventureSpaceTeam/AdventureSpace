using Content.Server.Paper;

namespace Content.Server.Nuke
{
    public sealed class NukeCodePaperSystem : EntitySystem
    {
        [Dependency] private readonly NukeCodeSystem _codes = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeCodePaperComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, NukeCodePaperComponent component, MapInitEvent args)
        {
            PaperComponent? paper = null;
            if (!Resolve(uid, ref paper))
                return;

            paper.Content += _codes.Code;
        }
    }
}
