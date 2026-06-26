using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.State;

// Hôte Blazor WebAssembly RÉEL : l'application s'exécute dans le navigateur. Tout le rendu est
// interactif côté client (pas de circuit serveur, pas de render mode à câbler — @onclick/@bind
// sont vivants par construction en WASM standalone).
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Client HTTP du front WASM. Le navigateur émet ses écritures (canal d'écriture) ET lit la grille
// (canal de lecture) vers l'API DISTANTE dont l'URL est CONFIGURABLE (clé « Api:BaseUrl », chargée
// depuis wwwroot/appsettings.json), et non plus vers son propre hôte. C'est le câblage runtime que
// le Sc.2 prouve. À défaut de config, on retombe sur l'hôte qui sert le WASM (HostEnvironment.BaseAddress).
builder.Services.AddScoped(_ =>
{
    var urlApi = builder.Configuration[ClientCanalEcriture.CleUrlApi];
    var baseAddress = string.IsNullOrWhiteSpace(urlApi) ? builder.HostEnvironment.BaseAddress : urlApi;
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

// État de session de consultation (rôle, enfant affiché) — par client navigateur.
builder.Services.AddScoped<SessionPlanning>();

await builder.Build().RunAsync();
