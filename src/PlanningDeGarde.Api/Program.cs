using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using PlanningDeGarde.Api;
using PlanningDeGarde.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// API explorable/documentée — OpenAPI natif .NET sur le canal d'écriture.
builder.Services.AddOpenApi();

// Diffusion temps réel (canal de lecture seule). L'hôte d'API héberge le hub : une écriture
// aboutie via le canal déclenche la diffusion vers les clients (front WASM dans le navigateur).
// Sans ce câblage, SignalRNotificateurPlanning (IHubContext<PlanningHub>) ne se résout pas et
// toute écriture passant par un handler qui notifie échoue à l'activation.
builder.Services.AddSignalR();

// CORS : autorise l'origine du front WASM (distinct de l'hôte API). L'origine est
// configurable (clé « Front:Origine ») ; à défaut, l'hôte de développement du front. La
// policy par défaut est configurée via les options résolues par DI (et non depuis une
// valeur capturée à la construction du builder) : le configurateur lit l'IConfiguration
// au moment où le middleware CORS matérialise ses options, ce qui respecte toute
// surcharge de configuration (ex. environnement de test), sinon l'origine resterait figée
// sur la valeur lue trop tôt et le front cross-origin serait refusé.
builder.Services.AddCors();
builder.Services.AddSingleton<IConfigureOptions<CorsOptions>, ConfigurationCorsOptions>();

// Application + Infrastructure (use cases, SignalR réel). La config foyer (acteurs) est persistée
// derrière les ports inchangés : Mongo durable si « Foyer:Persistance = Mongo », sinon InMemory.
builder.Services.AjouterPlanningDeGarde(builder.Configuration);

var app = builder.Build();

app.UseCors();

// Canal d'écriture requête/réponse (adaptateur de gauche) — commandes d'écriture en HTTP.
app.MapperCanalEcriture();

// Canal de lecture (adaptateur de droite, CQRS) — la grille agenda projetée, lue à distance par
// le front WASM (le navigateur n'a pas la projection en DI directe : il lit la grille via HTTP).
app.MapperCanalLecture();

// Flux OAuth externe (s28, volet 3) — démarrage (redirection authorize Google) + callback routé vers
// ConnexionOAuthHandler. Provider Google réel = dette de câblage (G3) ; logique prouvée par doublure (S9).
app.MapperOAuth();

// Canal de diffusion (lecture seule) — le front WASM s'y abonne dans le navigateur ; déclenché
// par une écriture aboutie, jamais l'inverse.
app.MapHub<PlanningHub>("/hubs/planning");

// API explorable — document OpenAPI + UI Scalar d'exploration interactive du canal. Servis
// INCONDITIONNELLEMENT (pas seulement sous IsDevelopment) : l'exploration est l'objet même de
// cet hôte ouvert, elle doit rester accessible quand l'API démarre en environnement headless
// (aucun front déployé ni référencé). Cf. Sc.4 (servabilité headless de la description).
app.MapOpenApi();
app.MapScalarApiReference();

// AUCUN amorçage runtime (Sc.8, s15) : l'hôte démarre sans seed. Sur un store Mongo vierge,
// l'application ouvre totalement vide (ni acteurs, ni slots/périodes/transferts, ni cycle de fond) ;
// dès qu'on saisit, c'est durable et rechargé aux lancements suivants. Les défauts InMemory restent
// portés par les adaptateurs eux-mêmes (config foyer), conservés pour la non-régression.

app.Run();

/// <summary>
/// Configure la policy CORS par défaut à partir de l'<see cref="IConfiguration"/> résolue par DI.
/// Construit de façon différée par le middleware CORS (au premier passage), donc après que toute
/// surcharge de configuration a été matérialisée : l'origine du front (« Front:Origine ») reflète
/// la configuration effective de l'hôte plutôt qu'une valeur lue trop tôt à la construction.
/// </summary>
internal sealed class ConfigurationCorsOptions(IConfiguration configuration) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var origineFront = configuration["Front:Origine"] ?? "https://localhost:7100";
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(origineFront)
            .AllowAnyHeader()
            .AllowAnyMethod());
    }
}

/// <summary>
/// Point d'entrée rendu accessible (partial public) pour que les tests d'intégration
/// (<c>WebApplicationFactory&lt;ApiProgram&gt;</c>) puissent démarrer l'hôte d'API détaché.
/// </summary>
public partial class ApiProgram { }
