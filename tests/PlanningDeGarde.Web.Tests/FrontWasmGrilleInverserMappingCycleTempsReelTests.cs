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
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ IHM, <c>@nominal</c>) — VOLET RUNTIME (le vrai symptôme PO) :
/// sur l'app réellement câblée (front WASM + API distante réelle + hub SignalR réel), un cycle de fond
/// N=2 est défini depuis l'<b>écran de configuration</b> (<see cref="ConfigurationFoyer"/>) — index pair (0)
/// → <c>parent-a</c> (Alice bleu), index impair (1) → <c>parent-b</c> (Bruno orange). La <b>vraie</b> grille
/// (<see cref="PlanningPartage"/>) câblée à la <b>MÊME API distante</b> affiche, sur la semaine ISO 28 (paire,
/// 06–12/07/2026), le fond résolu Parent A « Alice » en bleu.
///
/// Quand un parent <b>inverse le mapping</b> depuis la configuration (pair → <c>parent-b</c>, impair →
/// <c>parent-a</c>) et valide, la grille — <b>sans aucun rechargement</b> (même instance rendue, aucun second
/// render déclenché manuellement) — passe la case du 06/07 à Parent B « Bruno » en orange, <b>en case comme
/// en légende</b>, propagé par la <b>diffusion temps réel SignalR</b> déclenchée par l'écriture aboutie.
///
/// Anti « vert qui ment » : le baseline « Alice bleu » est asserté AVANT l'inversion, pour que la bascule
/// vers « Bruno orange » soit réellement observée (la case a bien changé, ce n'est pas un faux-vert). Le
/// chemin transite réellement — POST <c>/api/canal/definir-cycle</c> écrase le store cycle réel, le handler
/// déclenche la diffusion, le front (client SignalR réel redirigé vers le TestServer) re-projette via le
/// canal de lecture HTTP réel. Un bUnit à doublure ne prouverait ni la DI réelle, ni le chemin HTTP
/// d'écriture du cycle, ni la diffusion temps réel « sans rechargement ». Compose le câblage existant
/// (définition du cycle Sc.1/Sc.8 + diffusion SignalR palier 1 + relecture de grille) : aucun code de
/// production neuf attendu (early-green de câblage ; non-vacuité prouvée par le baseline ET par
/// neutralisation temporaire de l'abonnement au hub → rouge sur la bascule, cf. suivi Sc.3).
/// </summary>
public sealed class FrontWasmGrilleInverserMappingCycleTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Mettre_a_jour_la_grille_vers_Parent_B_orange_sur_la_semaine_ISO_28_sans_rechargement_When_un_parent_inverse_le_mapping_du_cycle_depuis_la_configuration()
    {
        // Given — l'API distante réelle (store vierge : aucune période, aucun cycle ; référentiel + palette
        // réels du foyer : parent-a « Alice » bleu, parent-b « Bruno » orange). L'écran de configuration ET
        // la grille sont câblés à cette MÊME API distante (même store cycle singleton, même hub SignalR réel).
        // Tous les services sont enregistrés AVANT le moindre rendu (contrainte du TestServiceProvider).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var config = RenderComponent<ConfigurationFoyer>();
        // … le cycle de fond est désormais sous l'onglet « Période de garde » (Sc.2, s20) : on l'active.
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config);

        // … un parent définit un cycle de 2 semaines : index pair (0) → parent-a, index impair (1) → parent-b,
        // et valide. L'écriture transite par le canal HTTP réel vers l'API distante (store cycle réel écrit).
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("2"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-0']").Change("parent-a"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-b"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));

        // … la grille réellement câblée à la MÊME API distante est affichée à la date de référence lundi
        // 29/06/2026 — sans aucune saisie de période. Son chargement (GET HTTP réel) est asynchrone.
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));

        // … baseline (anti faux-vert) : la semaine ISO 28 (paire, index 0) résout le fond Parent A « Alice »
        // en bleu, en case comme en légende.
        var caseInitiale = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
        Assert.Equal("Alice", caseInitiale.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseInitiale.GetAttribute("data-couleur"));
        AssertLegende(grille, "Alice", "bleu");

        // When — un parent inverse le mapping depuis la configuration : index pair (0) → parent-b, index
        // impair (1) → parent-a, et valide (ré-définition complète, dernière écriture gagne). Émission via le
        // canal d'écriture HTTP réel → le handler écrase le store cycle ET déclenche la diffusion temps réel.
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-0']").Change("parent-b"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-a"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));

        // … la connexion SignalR du front (long polling vers le TestServer) s'établit de façon asynchrone : on
        // ré-émet la diffusion en boucle de fond (idempotente — le store cycle est déjà inversé) pour qu'un
        // push tombe forcément APRÈS l'établissement de la connexion, sans dépendre du timing.
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
            // Then — sans aucun rechargement (même instance de grille rendue, aucun second render manuel), la
            // case du 06/07 (ISO 28 paire, désormais index 0 → parent-b) passe à Parent B « Bruno » en orange,
            // en case comme en légende — la grille suit l'inversion par diffusion SignalR.
            grille.WaitForAssertion(
                () =>
                {
                    var caseInversee = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
                    Assert.Equal("Bruno", caseInversee.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("orange", caseInversee.GetAttribute("data-couleur"));
                    AssertLegende(grille, "Bruno", "orange");
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    /// <summary>Asserte que la légende de la grille contient l'entrée <paramref name="nom"/> avec la couleur
    /// <paramref name="couleur"/>, résolue sur l'identifiant stable (« en case comme en légende »).</summary>
    private static void AssertLegende(IRenderedComponent<PlanningPartage> grille, string nom, string couleur)
    {
        var entree = grille.FindAll("[data-testid='legende-entree']")
            .Single(e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == nom);
        Assert.Equal(couleur, entree.GetAttribute("data-couleur"));
    }
}
