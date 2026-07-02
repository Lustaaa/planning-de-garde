using System;
using System.Linq;
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
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>) — VOLET RUNTIME de la couche de
/// résolution du fond : depuis l'<b>écran de configuration réellement câblé</b>
/// (<see cref="ConfigurationFoyer"/>), un parent définit un cycle de fond de 2 semaines
/// (index 0 → <c>parent-a</c>, index 1 → <c>parent-b</c>) et valide. <b>Sans aucune saisie de
/// période</b>, la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à la <b>même API
/// distante réelle</b> (<see cref="ApiDistanteFactory"/> — store cycle InMemory singleton partagé,
/// projection <c>GrilleAgendaQuery</c>, référentiel + palette réels du foyer) affiche le
/// <b>responsable de fond résolu</b> : la semaine ISO 27 (impaire) porte « Bruno » en orange,
/// la semaine ISO 28 (paire) « Alice » en bleu, <b>en case comme en légende</b>, et l'alternance
/// se poursuit sur les semaines suivantes (ISO 29 → Bruno, ISO 30 → Alice).
///
/// Anti « vert qui ment » : tout le chemin transite réellement — le POST <c>/api/canal/definir-cycle</c>
/// écrit le store cycle réel via le canal HTTP, et la grille relit ce store via le canal de lecture
/// HTTP réel ; le nom et la couleur rendus proviennent du référentiel réel résolu côté API sur
/// l'<b>identifiant stable</b> (jamais le libellé, règle 19). Si la grille ignorait le cycle (jours
/// sans période = gris neutre, nom vide), aucun fond n'apparaîtrait → rouge. Un bUnit à doublure de
/// transport ne prouverait ni la DI réelle, ni le chemin HTTP d'écriture du cycle, ni la projection.
/// </summary>
public sealed class FrontWasmGrilleCycleDeFondParPariteIsoTempsReelTests : TestContext
{
    [Fact]
    public void Should_Afficher_le_responsable_de_fond_par_parite_ISO_en_case_et_en_legende_sans_aucune_saisie_de_periode_When_un_parent_definit_un_cycle_de_fond_de_deux_semaines_depuis_la_configuration()
    {
        // Given — l'API distante réelle (store vierge : aucune période, aucun cycle ; référentiel +
        // palette réels du foyer : parent-a « Alice » bleu, parent-b « Bruno » orange). L'écran de
        // configuration ET la grille sont câblés à cette MÊME API distante (même store cycle singleton).
        // Tous les services sont enregistrés AVANT le moindre rendu (contrainte du TestServiceProvider) :
        // les deux composants partagent le même canal HTTP réel et la même horloge figée.
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

        // … le cycle de fond est désormais sous l'onglet « Période de garde » (Sc.2, s20) : on l'active,
        // puis le formulaire est présent et propose deux index de semaine (N = 2 par défaut).
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config);

        // When — un parent définit un cycle de 2 semaines : index pair (0) → parent-a, index impair
        // (1) → parent-b, et valide. L'écriture transite par le canal HTTP réel vers l'API distante.
        config.Find("[data-testid='champ-nombre-semaines']").Change("2");
        config.Find("[data-testid='champ-cycle-index-0']").Change("parent-a");
        config.Find("[data-testid='champ-cycle-index-1']").Change("parent-b");
        config.Find("#form-cycle").Submit();

        // … la définition aboutit (confirmation affichée) → le store cycle réel porte désormais le cycle.
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));

        // … la grille réellement câblée à la MÊME API distante est affichée à la date de référence
        // lundi 29/06/2026 (ISO 27, impaire) — sans aucune saisie de période. Son chargement (GET HTTP
        // réel vers l'API distante) est asynchrone : on attend que la fenêtre par défaut soit projetée
        // (28 cases, 4 semaines glissantes — re-pointé du 5 → 4 semaines par Sc.3).
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));

        // Then — la semaine ISO 27 (impaire, index 1) résout le fond Parent B : « Bruno » en orange.
        var caseIso27 = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Equal("Bruno", caseIso27.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseIso27.GetAttribute("data-couleur"));

        // … la semaine ISO 28 (paire, index 0) résout le fond Parent A : « Alice » en bleu.
        var caseIso28 = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
        Assert.Equal("Alice", caseIso28.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseIso28.GetAttribute("data-couleur"));

        // … et l'alternance se poursuit sur la fenêtre, sans nouvelle saisie : ISO 29 → Bruno, ISO 30 → Alice.
        var caseIso29 = GrilleRuntimeHarness.CaseDuJour(grille, "13/07");
        Assert.Equal("Bruno", caseIso29.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var caseIso30 = GrilleRuntimeHarness.CaseDuJour(grille, "20/07");
        Assert.Equal("Alice", caseIso30.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // … la légende suit « en case comme en légende » : exactement Bruno (orange) ET Alice (bleu),
        // dédoublonnés par identifiant stable (chacun couvre plusieurs semaines de la fenêtre).
        var legende = grille.FindAll("[data-testid='legende-entree']");
        Assert.Equal(2, legende.Count);
        var bruno = legende.Single(e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Bruno");
        Assert.Equal("orange", bruno.GetAttribute("data-couleur"));
        var alice = legende.Single(e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Alice");
        Assert.Equal("bleu", alice.GetAttribute("data-couleur"));
    }
}
