using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using NeuralBridge.SQL;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

string mistralKey = config["MistralApiKey"]!;
string neonDb = config.GetConnectionString("NeonDb")!;

var builder = Kernel.CreateBuilder();

// --- CONFIGURATION MISTRAL ---
builder.AddOpenAIChatCompletion(
    modelId: "open-mistral-7b",
    apiKey: mistralKey,
    endpoint: new Uri("https://api.mistral.ai/v1")
);

// --- AJOUT DU PLUGIN NEON ---
builder.Plugins.AddFromObject(new DatabasePlugin(neonDb), "NeonManager");

var kernel = builder.Build();

// Activation de l'appel automatique aux fonctions (SQL)
OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

Console.WriteLine("🇫🇷 NeuralBridge (Mistral Edition) connecté à Neon !");
Console.WriteLine("Pose-moi une question sur tes données :");

while (true)
{
    Console.Write("\n> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    var response = await kernel.InvokePromptAsync(input, new(settings));
    Console.WriteLine($"\nIA : {response}");
}