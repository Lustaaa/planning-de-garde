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
/// Sprint 20 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, iso-fonctionnel) : sous l'onglet
/// « Période de garde » (activé), la section <b>cycle de fond</b> existante aboutit <b>exactement comme
/// avant la refonte</b> (réutilise le handler <c>DefinirCycle</c>, aucun handler neuf), et la <b>vraie</b>
/// grille (<see cref="PlanningPartage"/>) câblée à la <b>même API distante</b> résout le responsable de
/// fond selon la <b>priorité inchangée : surcharge &gt; fond &gt; neutre</b> (palier 10) :
/// <list type="bullet">
///   <item>un jour couvert par une <b>période explicite</b> affiche cette <b>surcharge</b> (elle gagne sur le fond) ;</item>
///   <item>un jour sans période, sur un index de cycle <b>mappé</b>, affiche le <b>fond</b> résolu par parité ISO ;</item>
///   <item>un jour sans période, sur un index de cycle <b>non mappé</b>, retombe sur la teinte <b>neutre</b> (aucun nom).</item>
/// </list>
///
/// Rempart de non-régression du réagencement en onglets (Sc.2) : si le panneau « Période de garde »
/// n'avait pas correctement recâblé le formulaire de cycle, la définition n'aboutirait pas / la grille ne
/// résoudrait pas le fond → rouge. Le cycle est écrit via le canal HTTP réel puis relu par la grille via le
/// canal de lecture HTTP réel (aucune doublure) ; nom et couleur sont résolus sur l'identifiant stable.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigOngletPeriodeGardeCycleIsoFonctionnelTempsReelTests : TestContext
{
    [Fact]
    public void Should_Resoudre_le_fond_par_parite_ISO_avec_priorite_surcharge_puis_fond_puis_neutre_When_je_definis_le_cycle_depuis_l_onglet_Periode_de_garde()
    {
        // Given — l'API distante réelle (référentiel + palette réels : parent-a « Alice » bleu, parent-b
        // « Bruno » orange). Une SURCHARGE explicite : parent-b garde le 06/07 (semaine ISO 28, paire) —
        // ce jour devra afficher la surcharge (Bruno), pas le fond. L'écran de configuration ET la grille
        // partagent la MÊME API distante (même store cycle singleton). Services enregistrés avant tout rendu.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 6), new DateTime(2026, 7, 6));

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

        // … depuis l'onglet « Période de garde » (Sc.2, s20), un parent définit un cycle N = 2 :
        // index pair (0) → parent-a (Alice), index impair (1) LAISSÉ NON MAPPÉ (— aucun —, teinte neutre).
        var config = RenderComponent<ConfigurationFoyer>();
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config);
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("2"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-0']").Change("parent-a"));
        // index 1 non touché → reste « — aucun (teinte neutre) — » : les semaines ISO impaires seront neutres.
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));

        // When — la grille réellement câblée à la MÊME API est affichée à la date de référence lundi
        // 29/06/2026 (chargement GET HTTP réel : elle relit le cycle déjà écrit dans le store).
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));

        // Then (surcharge > fond) — le 06/07 (ISO 28, paire, index 0 → fond parent-a) est COUVERT par la
        // période explicite parent-b : la surcharge gagne, la case affiche « Bruno » en orange.
        var caseSurcharge = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
        Assert.Equal("Bruno", caseSurcharge.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseSurcharge.GetAttribute("data-couleur"));

        // Then (fond) — le 20/07 (ISO 30, paire, index 0), sans période explicite, résout le FOND parent-a :
        // « Alice » en bleu (résolution par parité ISO, inchangée).
        var caseFond = GrilleRuntimeHarness.CaseDuJour(grille, "20/07");
        Assert.Equal("Alice", caseFond.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseFond.GetAttribute("data-couleur"));

        // Then (neutre) — le 29/06 (ISO 27, impaire, index 1 NON MAPPÉ), sans période explicite, retombe sur
        // la teinte NEUTRE : aucun nom de responsable rendu (fond > neutre respecté, l'index non mappé n'invente
        // aucun responsable).
        var caseNeutre = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Null(caseNeutre.QuerySelector("[data-testid='nom-responsable']"));
    }
}
