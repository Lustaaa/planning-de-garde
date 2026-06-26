using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// API explorable/documentée (invariant non-codant) — OpenAPI/Swagger sur le canal d'écriture.
builder.Services.AddOpenApi();

// Application + Infrastructure (persistance en mémoire, SignalR réel, use cases).
builder.Services.AjouterPlanningDeGarde();

// Client HTTP du canal d'écriture : les vues d'écriture émettent leurs commandes via les
// endpoints `/api/canal/*` de l'API DISTANTE (adaptateur de gauche), PAS en appelant les handlers
// en DI direct. BaseAddress = URL d'API CONFIGURABLE (clé « Api:BaseUrl », cf. Sc.2) — le front
// consomme l'hôte d'API détaché, non plus son propre hôte (nav.BaseUri). À défaut de config, on
// retombe sur l'hôte courant (parité avec le rendu serveur pendant la transition WASM).
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    if (!string.IsNullOrWhiteSpace(configuration[ClientCanalEcriture.CleUrlApi]))
        return ClientCanalEcriture.Construire(configuration);

    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// État de session de consultation (rôle, enfant affiché) — par circuit Blazor.
builder.Services.AddScoped<PlanningDeGarde.Web.State.SessionPlanning>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<PlanningHub>("/hubs/planning");

// Canal d'écriture requête/réponse (adaptateur de gauche) — commandes d'écriture en HTTP.
app.MapperCanalEcriture();

// API explorable (invariant non-codant) — document OpenAPI du canal exposé en développement.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Données de démonstration — l'IHM s'ouvre peuplée plutôt que vide.
// Persistance en mémoire (aucune base réelle) : amorçage systématique en local.
// Désactivé sous l'environnement « Testing » : les tests d'intégration du canal observent
// un store vierge (sinon les périodes/slots de démo polluent la projection réelle observée).
if (!app.Environment.IsEnvironment("Testing"))
    app.AmorcerDonneesDemo();

app.Run();

/// <summary>
/// Point d'entrée rendu accessible (partial public) pour que les tests d'intégration
/// (<c>WebApplicationFactory&lt;Program&gt;</c>) puissent démarrer l'hôte Web réel.
/// </summary>
public partial class Program { }
