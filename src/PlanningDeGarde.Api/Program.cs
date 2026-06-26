using PlanningDeGarde.Api;
using PlanningDeGarde.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// API explorable/documentée — OpenAPI natif .NET sur le canal d'écriture.
builder.Services.AddOpenApi();

// CORS : autorise l'origine du front WASM (distinct de l'hôte API). L'origine est
// configurable (clé « Front:Origine ») ; à défaut, l'hôte de développement du front.
var origineFront = builder.Configuration["Front:Origine"] ?? "https://localhost:7100";
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(origineFront)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Application + Infrastructure (persistance en mémoire, SignalR réel, use cases).
builder.Services.AjouterPlanningDeGarde();

var app = builder.Build();

app.UseCors();

// Canal d'écriture requête/réponse (adaptateur de gauche) — commandes d'écriture en HTTP.
app.MapperCanalEcriture();

// API explorable — document OpenAPI + UI Scalar d'exploration interactive du canal.
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();

/// <summary>
/// Point d'entrée rendu accessible (partial public) pour que les tests d'intégration
/// (<c>WebApplicationFactory&lt;ApiProgram&gt;</c>) puissent démarrer l'hôte d'API détaché.
/// </summary>
public partial class ApiProgram { }
