using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared.Corvax.CCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server.White.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    private static readonly Gauge CachedCount = Metrics.CreateGauge(
        "tts_cached_count",
        "Amount of cached TTS audio.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, byte[]?> _cache = new();
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
            _apiToken = v;
        }, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes</returns>
    /// <exception cref="Exception">Throws if url or token CCVar not set or http request failed</exception>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text, string pitch, string rate, string? effect = null)
    {
        var url = _cfg.GetCVar(CCCVars.TTSApiUrl);
        var maxCacheSize = _cfg.GetCVar(CCCVars.TTSMaxCache);


        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("TTS Api url not specified");
        }

        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        var body = new GenerateVoiceRequest
        {
            Text = text,
            Speaker = speaker,
            Pitch = pitch,
            Rate = rate,
            Effect = effect
        };

        var request = CreateRequestLink(url, body);

        var reqTime = DateTime.UtcNow;
        var tries = 0;
        request:
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(request, cts.Token);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;
                if (tries < 3)
                {
                    goto request;
                }

                throw new Exception($"TTS request returned bad status code: {response.StatusCode}");
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(cts.Token);

            if (_cache.Count > maxCacheSize)
            {
                _cache.Remove(_cache.Last().Key);
            }

            _cache.Add(cacheKey, soundData);
            CachedCount.Inc();

            _sawmill.Debug($"Generated new sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Timeout of request generation new sound for '{text}' speech by '{speaker}' speaker");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            return null;
        }
    }

    private static string CreateRequestLink(string url, GenerateVoiceRequest body)
    {
        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["speaker"] = body.Speaker;
        query["text"] = body.Text;
        query["pitch"] = body.Pitch;
        query["rate"] = body.Rate;
        query["file"] = "1";
        query["ext"] = "ogg";
        if (body.Effect != null)
            query["effect"] = body.Effect;

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    public void ResetCache()
    {
        _cache.Clear();
        CachedCount.Set(0);
    }

    private string GenerateCacheKey(string speaker, string text)
    {
        var key = $"{speaker}/{text}";
        byte[] keyData = Encoding.UTF8.GetBytes(key);
        var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    private record GenerateVoiceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = default!;

        [JsonPropertyName("pitch")]
        public string Pitch { get; set; } = default!;

        [JsonPropertyName("rate")]
        public string Rate { get; set; } = default!;

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }
    }

    private struct GenerateVoiceResponse
    {
        [JsonPropertyName("results")]
        public List<VoiceResult> Results { get; set; }

        [JsonPropertyName("original_sha1")]
        public string Hash { get; set; }
    }

    private struct VoiceResult
    {
        [JsonPropertyName("audio")]
        public string Audio { get; set; }
    }
}
