using System;
using System.Linq;
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
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@limite</c>) — VOLET RUNTIME (le vrai symptôme PO) :
/// sur l'app réellement câblée (front WASM + API distante réelle + <b>store cycle singleton partagé</b> +
/// hub SignalR réel), <b>deux écrans</b> (deux parents) règlent <b>successivement le même index pair</b>
/// du cycle de fond, l'un après l'autre, depuis <b>deux navigateurs / DI séparés</b> (deux
/// <see cref="TestContext"/> distincts) câblés à la <b>MÊME</b> <see cref="ApiDistanteFactory"/>.
///
/// Écran 1 définit le cycle N=2 (index pair 0 → <c>parent-a</c> « Alice », index impair 1 → <c>parent-b</c>
/// « Bruno ») ; les deux grilles affichent alors « Alice » sur la semaine ISO 28 (paire, 06–12/07/2026).
/// Écran 2, juste après, règle l'index pair sur <c>parent-c</c> (« Marie-Hélène Grand-Dubois ») — chacun via
/// le <b>canal d'écriture HTTP réel</b> (<c>POST /api/canal/definir-cycle</c>, règle 27). La <b>dernière
/// écriture gagne</b> (le store cycle écrase par affectation, sans version ni rejet — décision CP, patron
/// s08 Sc.7) : <b>aucune</b> des deux éditions n'est rejetée (confirmation affichée sur chaque écran), et les
/// <b>DEUX</b> grilles convergent vers « Marie-Hélène Grand-Dubois » sur l'index pair (ISO 28), en case comme
/// en légende, propagé par la <b>diffusion temps réel SignalR</b>, <b>sans aucun rechargement</b>.
///
/// Anti « vert qui ment » : le baseline « Alice » est asserté sur les DEUX grilles AVANT l'édition de l'écran 2,
/// pour que la convergence vers « Marie-Hélène Grand-Dubois » soit réellement observée (la case a bien changé,
/// ce n'est pas un faux-vert). Un bUnit à doublure ne prouverait ni la mémoire partagée du store cycle singleton
/// serveur, ni les deux clients SignalR, ni le chemin d'écriture HTTP du cycle. Compose le câblage existant
/// (définition du cycle Sc.1 + re-définition diffusée Sc.3 + store partagé/convergence s08 Sc.7) : aucun code de
/// production neuf attendu (early-green de câblage ; non-vacuité prouvée par le baseline « Alice » ET par
/// neutralisation temporaire de l'abonnement au hub → rouge sur la convergence, cf. suivi Sc.6).
///
/// <para><b>Note référentiel réel.</b> Le scénario décrit l'acteur gagnant « Parent C vert » (doublure backend) ;
/// le référentiel <b>réel</b> du foyer résout <c>parent-c</c> en « Marie-Hélène Grand-Dubois » (acteur au nom
/// long, bleu). Le runtime prouve la convergence sur le référentiel réel, par le <b>nom</b> résolu sur
/// l'identifiant stable (jamais le libellé, règle 19) : « Alice » → « Marie-Hélène Grand-Dubois », signal de
/// bascule non ambigu en case comme en légende.</para>
/// </summary>
public sealed class FrontWasmGrilleDeuxEcransCycleDerniereEcritureGagneTempsReelTests : TestContext
{
    private const string NomAlice = "Alice";
    private const string NomMarieHelene = "Marie-Hélène Grand-Dubois"; // parent-c résolu sur le référentiel réel

    [Fact]
    public async Task Should_Faire_converger_les_deux_grilles_vers_Parent_C_sur_l_index_pair_sans_rejet_ni_rechargement_When_deux_parents_reglent_le_meme_index_l_un_apres_l_autre_depuis_deux_ecrans()
    {
        // Given — UNE seule API distante réelle (store cycle singleton partagé + hub SignalR réel ; store
        // vierge : aucune période, aucun cycle ; référentiel + palette réels du foyer). Écran 1 = ce
        // TestContext ; écran 2 = un second TestContext (navigateur / DI séparés), câblé à la MÊME API → même
        // store cycle partagé côté serveur, deux clients SignalR distincts. Tous les services de chaque
        // contexte sont enregistrés AVANT le moindre rendu (contrainte du TestServiceProvider).
        using var api = new ApiDistanteFactory();
        ConfigurerCanal(this, api);
        using var ecran2 = new TestContext();
        ConfigurerCanal(ecran2, api);

        // … écran 1 définit le cycle N=2 : index pair (0) → parent-a (« Alice »), index impair (1) → parent-b
        // (« Bruno »), et valide. L'écriture transite par le canal HTTP réel → le store cycle réel porte ce
        // mapping. (« Écran 1 règle l'index pair sur Parent A. »)
        var config1 = RenderComponent<ConfigurationFoyer>();
        // … le cycle de fond est désormais sous l'onglet « Période de garde » (Sc.2, s20) : on l'active.
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config1);
        DefinirCycle(config1, indexPair: "parent-a", indexImpair: "parent-b");
        config1.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));
        Assert.Empty(config1.FindAll("[data-testid='motif-echec-cycle']")); // 1ʳᵉ écriture acceptée, aucun rejet

        // … les DEUX grilles, câblées à la MÊME API distante, sont affichées à la date de référence lundi
        // 29/06/2026 — sans aucune saisie de période. Leur chargement (GET HTTP réel) est asynchrone.
        var grille1 = RendreGrille(this);
        var grille2 = RendreGrille(ecran2);

        // … baseline (anti faux-vert) asserté sur les DEUX grilles : la semaine ISO 28 (paire, index 0) résout
        // le fond Parent A « Alice », en case comme en légende.
        AssertNomDans(grille1, NomAlice);
        AssertNomDans(grille2, NomAlice);

        // When — écran 2, juste après, règle l'index pair (0) sur parent-c (« Marie-Hélène Grand-Dubois »),
        // l'impair (1) restant parent-b, et valide (même store partagé, dernière écriture gagne). Émission via
        // le canal d'écriture HTTP réel → le handler écrase le store cycle ET déclenche la diffusion temps réel.
        var config2 = ecran2.RenderComponent<ConfigurationFoyer>();
        // … le cycle de fond est sous l'onglet « Période de garde » (Sc.2, s20) : on l'active sur l'écran 2.
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config2);
        DefinirCycle(config2, indexPair: "parent-c", indexImpair: "parent-b");
        config2.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));
        Assert.Empty(config2.FindAll("[data-testid='motif-echec-cycle']")); // 2ᵉ écriture acceptée (pas de jeton optimiste)

        // … la connexion SignalR de chaque grille (long polling vers le TestServer) s'établit de façon
        // asynchrone : on ré-émet la diffusion en boucle de fond (idempotente — le store cycle est déjà inversé)
        // pour qu'un push tombe forcément APRÈS l'établissement des deux connexions, sans dépendre du timing.
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseurDeDiffusion = Task.Run(async () =>
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
            // Then — la dernière écriture gagne : sans aucun rechargement (mêmes instances de grilles rendues,
            // aucun second render manuel), les DEUX grilles convergent vers Parent C « Marie-Hélène Grand-Dubois »
            // sur la semaine ISO 28 (index pair), en case comme en légende, propagé par la diffusion SignalR ;
            // aucune édition rejetée.
            grille1.WaitForAssertion(() => AssertNomDans(grille1, NomMarieHelene), TimeSpan.FromSeconds(15));
            grille2.WaitForAssertion(() => AssertNomDans(grille2, NomMarieHelene), TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    /// <summary>Enregistre, dans le contexte donné, le câblage du front vers l'API distante réelle (client
    /// HTTP réel, session, horloge figée, hub SignalR redirigé vers le TestServer) — AVANT tout rendu.</summary>
    private static void ConfigurerCanal(Bunit.TestContext ctx, ApiDistanteFactory api)
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

    /// <summary>Rend la grille réelle dans le contexte (services déjà enregistrés) à la date de référence et
    /// attend que la fenêtre par défaut soit projetée (28 cases-jour, 4 semaines glissantes — re-pointé
    /// du 5 → 4 semaines par Sc.3) — chargement GET HTTP réel asynchrone.</summary>
    private static IRenderedComponent<PlanningPartage> RendreGrille(Bunit.TestContext ctx)
    {
        var grille = ctx.RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        return grille;
    }

    /// <summary>Définit le cycle N=2 depuis l'écran de configuration : index pair (0) et impair (1) sur les
    /// identifiants stables donnés, puis soumet (canal d'écriture HTTP réel).</summary>
    private static void DefinirCycle(IRenderedComponent<ConfigurationFoyer> config, string indexPair, string indexImpair)
    {
        config.Find("[data-testid='champ-nombre-semaines']").Change("2");
        config.Find("[data-testid='champ-cycle-index-0']").Change(indexPair);
        config.Find("[data-testid='champ-cycle-index-1']").Change(indexImpair);
        config.Find("#form-cycle").Submit();
    }

    /// <summary>Asserte que la semaine ISO 28 (06/07, paire) ET la légende d'une grille portent
    /// <paramref name="nom"/>, résolu sur l'identifiant stable (« en case comme en légende »).</summary>
    private static void AssertNomDans(IRenderedComponent<PlanningPartage> grille, string nom)
    {
        Assert.Equal(nom, GrilleRuntimeHarness.CaseDuJour(grille, "06/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Contains(grille.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == nom);
    }
}
