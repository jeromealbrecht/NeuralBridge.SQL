using System.Web;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace NeuralBridge.SQL.Services;

public class AudioDownloader
{
    private readonly YoutubeClient _youtube;

    public AudioDownloader()
    {
        _youtube = new YoutubeClient(new HttpClient(new LoggingHttpHandler()));
    }

    public async Task DownloadAsync(string videoUrl, string outputPath)
    {
        string cleanUrl = ExtractVideoUrl(videoUrl);
        Console.WriteLine($"[YouTube] URL nettoyée : {cleanUrl}");

        Console.WriteLine("[YouTube] Récupération du manifest...");
        var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(cleanUrl);

        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate()
            ?? throw new InvalidOperationException("Aucun flux audio trouvé pour cette vidéo.");

        Console.WriteLine($"[YouTube] Stream sélectionné : {streamInfo.Bitrate} | {streamInfo.Container} | {streamInfo.Url[..80]}...");

        await _youtube.Videos.Streams.DownloadAsync(streamInfo, outputPath);
    }

    private static string ExtractVideoUrl(string url)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        string? videoId = query["v"];
        return !string.IsNullOrWhiteSpace(videoId)
            ? $"https://www.youtube.com/watch?v={videoId}"
            : url;
    }
}

internal class LoggingHttpHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[HTTP] {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, cancellationToken);
        Console.WriteLine($"[HTTP] {(int)response.StatusCode} {response.StatusCode} ← {request.RequestUri?.AbsolutePath}");
        return response;
    }
}
