using Microsoft.SemanticKernel;
using NeuralBridge.SQL.Infrastructure;
using NeuralBridge.SQL.Plugins;

var builder = WebApplication.CreateBuilder(args);

string mistralKey = builder.Configuration["MistralApiKey"]!;
string connectionString = builder.Configuration.GetConnectionString("NeonDb")!;

builder.Services.AddSingleton(new Repository(connectionString));
builder.Services.AddScoped<SpectralMatchPlugin>();

builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "open-mistral-7b",
        apiKey: mistralKey,
        endpoint: new Uri("https://api.mistral.ai/v1")
    );

    var plugin = sp.GetRequiredService<SpectralMatchPlugin>();
    kernelBuilder.Plugins.AddFromObject(plugin, "SpectralMatch");

    return kernelBuilder.Build();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
