using Microsoft.SemanticKernel;
using NeuralBridge.SQL.Infrastructure;
using NeuralBridge.SQL.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NeuralBridge.SQL.Plugins;

public class SpectralMatchPlugin
{
    private readonly AudioDownloader _downloader;
    private readonly Repository _repository;

    public SpectralMatchPlugin(Repository repository)
    {
        _downloader = new AudioDownloader();
        _repository = repository;
    }

    [KernelFunction]
    [Description("Télécharge uniquement la piste audio d'une vidéo YouTube et la sauvegarde dans un fichier local.")]
    public async Task<string> DownloadAudioFromYoutube(
        [Description("L'URL complète de la vidéo YouTube")] string videoUrl,
        [Description("Le chemin de destination du fichier audio (ex: C:/audio/titre.m4a)")] string outputPath)
    {
        try
        {
            await _downloader.DownloadAsync(videoUrl, outputPath);
            return $"Audio récupéré : {outputPath}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[YouTube] Exception complète : {ex}");
            return $"Erreur lors du téléchargement audio : {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Analyse un fichier audio et retourne les énergies des 4 bandes (low/mid/high/air) + snapshot FFT JSON. Résultat prêt pour SpectralSignatures.")]
    public string AnalyzeSpectralContent(
        [Description("Chemin vers le fichier audio local à analyser (ex: C:/audio/titre.m4a)")] string filePath)
    {
        try
        {
            return JsonSerializer.Serialize(SpectralAnalyzer.Analyze(filePath));
        }
        catch (Exception ex)
        {
            return $"Erreur lors de l'analyse spectrale : {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Analyse un fichier audio et sauvegarde sa signature spectrale complète (4 bandes + FFT JSON) dans la table SpectralSignatures de Neon.")]
    public async Task<string> SaveSpectralSignature(
        [Description("Nom de la référence (ex: 'Pop Vocal Pro')")] string name,
        [Description("URL source YouTube ou autre")] string sourceUrl,
        [Description("Chemin local du fichier audio à analyser")] string filePath)
    {
        try
        {
            var result = SpectralAnalyzer.Analyze(filePath);
            int id = await _repository.SaveSpectralSignatureAsync(name, sourceUrl, result);
            return $"Signature '{name}' sauvegardée (id={id})\n  low={result.LowEnergy:F2} dBFS | mid={result.MidEnergy:F2} dBFS | high={result.HighEnergy:F2} dBFS | air={result.AirEnergy:F2} dBFS\n  Pic sibilant : {result.HighPeakHz:F0} Hz";
        }
        catch (Exception ex)
        {
            return $"Erreur SaveSpectralSignature : {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Liste toutes les signatures spectrales de référence enregistrées dans Neon.")]
    public async Task<string> GetSpectralSignatures()
    {
        try
        {
            var lines = await _repository.GetSpectralSignaturesAsync();
            return lines.Count > 0 ? string.Join("\n", lines) : "Aucune signature enregistrée.";
        }
        catch (Exception ex)
        {
            return $"Erreur GetSpectralSignatures : {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Enregistre un traitement utilisateur dans la table UserProcessings (historique).")]
    public async Task<string> SaveUserProcessing(
        [Description("Identifiant de l'utilisateur")] string userId,
        [Description("Nom du fichier audio traité")] string inputFilename,
        [Description("Id de la signature de référence utilisée")] int referenceId,
        [Description("Réduction en dB appliquée par le De-esser")] float appliedReduction,
        [Description("true si le traitement a réussi, false sinon")] bool success)
    {
        try
        {
            int id = await _repository.SaveUserProcessingAsync(userId, inputFilename, referenceId, appliedReduction, success);
            return $"Traitement enregistré (id={id}) pour user={userId} | fichier={inputFilename} | réduction={appliedReduction}dB";
        }
        catch (Exception ex)
        {
            return $"Erreur SaveUserProcessing : {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Récupère le nombre total de lignes dans une table spécifique (ex: stocks).")]
    public async Task<string> GetTableCount(
        [Description("Le nom de la table à compter")] string tableName)
    {
        try
        {
            long count = await _repository.GetTableCountAsync(tableName);
            return $"La table {tableName} contient {count} enregistrements sur Neon.";
        }
        catch (Exception ex)
        {
            return $"Erreur GetTableCount : {ex.Message}";
        }
    }
}
