using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.Light.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._White.TTS;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.White.TTS;
using Content.Shared.GameTicking;
using Content.Shared.White;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.White.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TTSPitchRateSystem _ttsPitchRateSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;
    private string _apiUrl = string.Empty;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, url => _apiUrl = url, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<TTSAnnouncementEvent>(OnAnnounceRequest);

        SubscribeNetworkEvent<RequestGlobalTTSEvent>(OnRequestGlobalTTS);

        _netMgr.RegisterNetMessage<MsgRequestTTS>(OnRequestTTS);
    }

    private async void OnAnnounceRequest(TTSAnnouncementEvent ev)
    {
        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var ttsPrototype))
            return;

        var message = FormattedMessage.RemoveMarkup(ev.Message);
        var soundData = await GenerateTTS(null, message, ttsPrototype.Speaker, speechRate: "slow", effect: "announce");

        if (soundData == null)
            return;

        Filter filter;
        if (ev.Global)
            filter = Filter.Broadcast();
        else
        {
            var station = _stationSystem.GetOwningStation(ev.Source);
            if (station == null)
                return;

            if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
                return;

            filter = _stationSystem.GetInStation(stationDataComp);
        }

        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity != null)
            {
                // Get emergency lights in range to broadcast from
                var entities = _lookup.GetEntitiesInRange(player.AttachedEntity.Value, 30f)
                    .Where(HasComp<EmergencyLightComponent>)
                    .ToList();

                if (entities.Count == 0)
                    return;

                // Get closest emergency light
                var entity = entities.First();
                var range = new Vector2(100f);

                foreach (var item in entities)
                {
                    var itemSource = _xforms.GetWorldPosition(Transform(item));
                    var playerSource = _xforms.GetWorldPosition(Transform(player.AttachedEntity.Value));

                    var distance = playerSource - itemSource;

                    if (range.Length() > distance.Length())
                    {
                        range = distance;
                        entity = item;
                    }
                }

                RaiseNetworkEvent(new PlayTTSEvent(GetNetEntity(entity), soundData, true), Filter.SinglePlayer(player),
                    false);
            }
        }
    }

    private async void OnRequestGlobalTTS(RequestGlobalTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            ev.Text.Length > MaxMessageChars ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        var soundData = await GenerateTTS(null, ev.Text, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayGlobalTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnRequestTTS(MsgRequestTTS ev)
    {
        var url = _cfg.GetCVar(CCCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!_playerManager.TryGetSessionByChannel(ev.MsgChannel, out var session) ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        var soundData = await GenerateTTS(ev.Uid, ev.Text, protoVoice.Speaker);
        var entity = GetNetEntity(ev.Uid);

        if (!entity.IsValid())
        {
            return;
        }

        if (soundData != null)
        {
            RaiseNetworkEvent(new PlayTTSEvent(entity, soundData, false), Filter.SinglePlayer(session), false);
        }
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars)
            return;

        if (string.IsNullOrEmpty(_apiUrl))
        {
            return;
        }

        var voiceId = component.VoicePrototypeId;

        if (voiceId == null)
        {
            return;
        }

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        var message = FormattedMessage.RemoveMarkup(args.Message);

        var soundData = await GenerateTTS(uid, message, protoVoice.Speaker);
        if (soundData is null)
            return;
        var ttsEvent = new PlayTTSEvent(GetNetEntity(uid), soundData, false);

        // Say
        if (args.ObfuscatedMessage is null)
        {
            RaiseNetworkEvent(ttsEvent, Filter.Pvs(uid), false);
            return;
        }

        // Whisper
        var wList = new List<string>
        {
            "тсс",
            "псс",
            "ччч",
            "ссч",
            "сфч",
            "тст"
        };
        var chosenWhisperText = _random.Pick(wList);
        var obfSoundData = await GenerateTTS(uid, chosenWhisperText, protoVoice.Speaker);
        if (obfSoundData is null)
            return;
        var obfTtsEvent = new PlayTTSEvent(GetNetEntity(uid), obfSoundData, false);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;


        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
                continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
                continue;

            EntityEventArgs actualEvent;

            if (distance > ChatSystem.WhisperClearRange)
            {
                actualEvent = obfTtsEvent;
            }
            else
            {
                actualEvent = ttsEvent;
            }

            RaiseNetworkEvent(actualEvent, Filter.SinglePlayer(session), false);
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async Task<byte[]?> GenerateTTS(EntityUid? uid, string text, string speaker, string? speechPitch = null, string? speechRate = null, string? effect = null)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return null;

        string pitch;
        string rate;
        if (speechPitch == null || speechRate == null)
        {
            if (uid == null || !_ttsPitchRateSystem.TryGetPitchRate(uid.Value, out var pitchRate))
            {
                pitch = "medium";
                rate = "medium";
            }
            else
            {
                pitch = pitchRate[0];
                rate = pitchRate[1];
            }
        }
        else
        {
            pitch = speechPitch;
            rate = speechRate;
        }

        return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, pitch, rate, effect);
    }
}

public sealed class TransformSpeakerVoiceEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string VoiceId;

    public TransformSpeakerVoiceEvent(EntityUid sender, string voiceId)
    {
        Sender = sender;
        VoiceId = voiceId;
    }
}
