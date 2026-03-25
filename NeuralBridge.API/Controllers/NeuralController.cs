using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace NeuralBridge.API.Controllers;

[ApiController]
[Route("api/[controller]")] // L'URL sera : api/neural
public class NeuralController : ControllerBase
{
    private readonly Kernel _kernel;

    // Le Kernel est injecté automatiquement ici grâce au Program.cs !
    public NeuralController(Kernel kernel)
    {
        _kernel = kernel;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Le message est vide.");

        // Configuration pour que l'IA appelle tes fonctions de plugin toute seule
        OpenAIPromptExecutionSettings settings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        try
        {
            var prompt = $"""
                <message role="system">
                Tu es NeuralBridge, un assistant spécialisé dans l'analyse spectrale audio et le de-essing.
                Tu peux : télécharger l'audio d'une vidéo YouTube, analyser les bandes spectrales (low/mid/high/air),
                sauvegarder et consulter des signatures de référence, et enregistrer l'historique des traitements.
                Si l'utilisateur pose une question sans rapport avec ces fonctionnalités, réponds poliment
                que tu es uniquement conçu pour l'analyse spectrale et le traitement audio.
                </message>
                <message role="user">{request.Prompt}</message>
                """;

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(settings));

            return Ok(new
            {
                answer = result.ToString(),
                success = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// Le petit modèle pour lire le JSON envoyé par le front
public class AskRequest
{
    public string Prompt { get; set; } = string.Empty;
}