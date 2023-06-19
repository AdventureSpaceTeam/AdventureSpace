using System.Threading.Tasks;
using Content.Client.Replay.UI.Loading;
using Robust.Client.Replays.Loading;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Replay;

public sealed class ContentLoadReplayJob : LoadReplayJob
{
    private readonly LoadingScreen<bool> _screen;

    public ContentLoadReplayJob(
        float maxTime,
        IWritableDirProvider dir,
        ResPath path,
        IReplayLoadManager loadMan,
        LoadingScreen<bool> screen)
        : base(maxTime, dir, path, loadMan)
    {
        _screen = screen;
    }

    protected override async Task Yield(float value, float maxValue, LoadingState state, bool force)
    {
        var header = Loc.GetString("replay-loading", ("cur", (int)state + 1), ("total", 5));
        var subText = Loc.GetString(state switch
        {
            LoadingState.ReadingFiles => "replay-loading-reading",
            LoadingState.ProcessingFiles => "replay-loading-processing",
            LoadingState.Spawning => "replay-loading-spawning",
            LoadingState.Initializing => "replay-loading-initializing",
            _ => "replay-loading-starting",
        });
        _screen.UpdateProgress(value, maxValue, header, subText);

        await base.Yield(value, maxValue, state, force);
    }
}
