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

// Application + Infrastructure (persistance en mémoire, SignalR réel, use cases).
builder.Services.AjouterPlanningDeGarde();

var app = builder.Build();

app.UseCors();

// Canal d'écriture requête/réponse (adaptateur de gauche) — commandes d'écriture en HTTP.
app.MapperCanalEcriture();

// Canal de lecture (adaptateur de droite, CQRS) — la grille agenda projetée, lue à distance par
// le front WASM (le navigateur n'a pas la projection en DI directe : il lit la grille via HTTP).
app.MapperCanalLecture();

// Canal de diffusion (lecture seule) — le front WASM s'y abonne dans le navigateur ; déclenché
// par une écriture aboutie, jamais l'inverse.
app.MapHub<PlanningHub>("/hubs/planning");

// API explorable — document OpenAPI + UI Scalar d'exploration interactive du canal. Servis
// INCONDITIONNELLEMENT (pas seulement sous IsDevelopment) : l'exploration est l'objet même de
// cet hôte ouvert, elle doit rester accessible quand l'API démarre en environnement headless
// (aucun front déployé ni référencé). Cf. Sc.4 (servabilité headless de la description).
app.MapOpenApi();
app.MapScalarApiReference();

// Données de démonstration — la grille lue par le front WASM s'ouvre peuplée plutôt que vide.
// Persistance en mémoire (aucune base réelle) : amorçage systématique au démarrage de l'hôte d'API.
// Désactivé sous l'environnement « Testing » : les tests d'intégration observent un store vierge
// (sinon les périodes/slots de démo polluent la projection réelle observée).
if (!app.Environment.IsEnvironment("Testing"))
    app.AmorcerDonneesDemo();

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
