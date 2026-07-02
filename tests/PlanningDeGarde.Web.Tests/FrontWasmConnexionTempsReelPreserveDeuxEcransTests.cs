using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 23 — Sc.9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, 100 % temps réel SignalR) : la
/// <b>connexion/déconnexion</b> n'altère pas la propagation lecture. Deux écrans planning
/// (<see cref="PlanningPartage"/>) câblés à la MÊME API distante réelle (<see cref="ApiDistanteFactory"/>
/// unique → store singleton partagé, hub SignalR réel commun), l'un <b>connecté en tant que « Alice »</b>
/// (bandeau de connexion s23), l'autre non. Une modification de config (renommer l'acteur « Alice ») émise
/// via le canal d'écriture HTTP réel déclenche la diffusion SignalR : <b>les deux écrans convergent</b>
/// (grille + légende re-résolues au nouveau nom) <b>sans rechargement</b> — l'état de connexion d'un écran
/// n'altère pas la propagation lecture de l'autre (non-régression temps réel s20). Test multi-clients
/// SignalR → catégorie du flake TempsReel catalogué (dette P1, triage isolation en cas de rouge isolé).
/// </summary>
public sealed class FrontWasmConnexionTempsReelPreserveDeuxEcransTests : TestContext
{
    private const string ActeurAlice = "parent-a"; // seed s22 : parent-a = « Alice »
    private static readonly DateTime JourEnFenetre = new(2026, 6, 30); // mardi dans la fenêtre 4 semaines

    private static void ConfigurerEcranPlanning(Bunit.TestContext ctx, ApiDistanteFactory api)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(new SessionPlanning());
        ctx.Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        ctx.Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    private static IRenderedComponent<PlanningPartage> RendreEtAttendreGrille(Bunit.TestContext ctx)
    {
        var planning = ctx.RenderComponent<PlanningPartage>();
        planning.WaitForState(
            () => planning.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        return planning;
    }

    private static bool LegendeContient(IRenderedComponent<PlanningPartage> planning, string nom)
        => planning.FindAll("[data-testid='legende-entree'] .legende-nom").Any(n => n.TextContent.Trim() == nom);

    [Fact]
    public async Task La_connexion_d_un_ecran_n_altere_pas_la_convergence_temps_reel_de_l_autre_When_un_acteur_est_renomme_en_config()
    {
        // Given — UNE seule API distante réelle (store partagé) ; une période d'Alice (parent-a) dans la
        // fenêtre pour que la légende affiche « Alice » sur les deux écrans.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, ActeurAlice, JourEnFenetre, JourEnFenetre.AddHours(12));

        // Deux écrans planning distincts (deux DI) câblés à la même API réelle (hub SignalR commun).
        ConfigurerEcranPlanning(this, api);
        using var ecran2 = new TestContext();
        ConfigurerEcranPlanning(ecran2, api);

        var planning1 = RendreEtAttendreGrille(this);
        var planning2 = RendreEtAttendreGrille(ecran2);

        // … cohérence de base : la légende des deux écrans affiche « Alice » (anti faux-vert).
        planning1.WaitForAssertion(() => Assert.True(LegendeContient(planning1, "Alice")), TimeSpan.FromSeconds(10));
        planning2.WaitForAssertion(() => Assert.True(LegendeContient(planning2, "Alice")), TimeSpan.FromSeconds(10));

        // … l'écran 1 SE CONNECTE en tant qu'« Alice » (bandeau de connexion s23) : POST se-connecter réel.
        // Un compte Actif d'Alice est nécessaire pour connecter l'écran 1.
        api.Services.GetRequiredService<IEditeurComptes>().Creer("compte-alice", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        planning1.Find("[data-testid='champ-email-connexion']").Change("alice@foyer.fr");
        planning1.Find("[data-testid='bouton-se-connecter']").Click();
        planning1.WaitForAssertion(
            () => Assert.Contains("Alice", planning1.Find("[data-testid='etat-connexion']").TextContent),
            TimeSpan.FromSeconds(10));

        // When — un parent renomme « Alice » en « Aline » via le canal d'écriture HTTP réel (config) :
        // store partagé muté + diffusion temps réel déclenchée par le handler (EditerActeur notifie).
        using var clientConfig = GrilleRuntimeHarness.ClientVers(api);
        var reponse = await clientConfig.PostAsJsonAsync(
            "api/canal/editer-acteur", new CanalEcriture.EditerActeurRequete(ActeurAlice, Nom: "Aline"));
        reponse.EnsureSuccessStatusCode();

        // … re-diffusion de fond idempotente (store déjà muté) : le push SignalR tombe forcément APRÈS
        // l'établissement des connexions long polling, sans dépendre du timing (anti-flake).
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — sans rechargement, LES DEUX écrans convergent : la légende re-résolue affiche « Aline »
            // et non plus « Alice ». L'écran 1 (CONNECTÉ) converge comme l'écran 2 (non connecté) : la
            // connexion n'altère pas la propagation lecture (non-régression temps réel s20).
            planning1.WaitForAssertion(
                () =>
                {
                    Assert.True(LegendeContient(planning1, "Aline"));
                    Assert.False(LegendeContient(planning1, "Alice"));
                },
                TimeSpan.FromSeconds(15));
            planning2.WaitForAssertion(
                () =>
                {
                    Assert.True(LegendeContient(planning2, "Aline"));
                    Assert.False(LegendeContient(planning2, "Alice"));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
