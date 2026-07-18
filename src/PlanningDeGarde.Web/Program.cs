using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlanningDeGarde.Application;
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

// Horloge « du jour » : les formulaires d'écriture pré-remplissent leur date depuis ce port
// (jamais une date figée ni DateTime.Today en dur dans la vue). L'implémentation système lit
// l'horloge réelle du navigateur ; le double de test la fige pour le déterminisme. La grille de
// lecture s'en sert aussi comme date de référence de la fenêtre projetée (jamais DateTime.Now).
builder.Services.AddSingleton<IDateTimeProvider, HorlogeNavigateur>();

// Préférence de thème clair/sombre (Sc.3) : le switch persiste le choix et l'applique via le module JS
// window.pdgTheme (localStorage + data-theme). Adaptateur de bord, aucune règle métier.
builder.Services.AddScoped<IPreferencesTheme, PreferencesThemeJs>();

// Persistance de session (s31, Sc.1) : le jeton d'identité du compte connecté est écrit dans localStorage
// (window.pdgSession) au login et relu au DÉMARRAGE par le restaurateur, si bien que la session survit au
// F5 sans repasser par /connexion. La session elle-même reste en MÉMOIRE (SessionPlanning, borne R30) :
// seule l'amorce d'identité est persistée/rejouée. Adaptateur de bord, aucune règle métier.
builder.Services.AddScoped<IPersistanceSession, PersistanceSessionJs>();
builder.Services.AddScoped<RestaurateurSession>();

// Écoute Échap des modals de la Config foyer (finition PO s33) : capture au niveau DOCUMENT via le module JS
// window.pdgModal (attach à l'ouverture, detach à la fermeture). Échap = « Annuler » (ferme sans muter).
// Adaptateur de bord, aucune règle métier.
builder.Services.AddScoped<IEcouteurEchapModal, EcouteurEchapModalJs>();

// Écoute du RELÂCHEMENT du pointeur au niveau DOCUMENT (s49, correctif du gate G3) : la sélection de plage
// par drag est finalisée même si le bouton est relâché HORS d'une case, via le module JS window.pdgPointeur
// (document.addEventListener('pointerup')). Adaptateur de bord, aucune règle métier.
builder.Services.AddScoped<IEcouteurRelachementPointeur, EcouteurRelachementPointeurJs>();

// Écoute du MOUVEMENT du pointeur au niveau DOCUMENT (s49, 2ᵉ correctif du gate G3) : pendant un drag, la case
// sous le curseur est résolue par document.elementFromPoint (module JS window.pdgPointeur) et remontée au
// composant, qui met à jour le curseur de sélection — voie FIABLE, indépendante des @onpointerover par case
// (fragiles/manqués pendant un glisser). Adaptateur de bord, aucune règle métier.
builder.Services.AddScoped<IEcouteurMouvementPointeur, EcouteurMouvementPointeurJs>();

// Câblage de la connexion au hub SignalR de lecture. Neutre en WASM réel (le navigateur négocie
// en WebSocket vers l'API distante) ; surchargeable par un hôte de test pour pointer son TestServer.
builder.Services.AddSingleton(new OptionsConnexionHub());

await builder.Build().RunAsync();
