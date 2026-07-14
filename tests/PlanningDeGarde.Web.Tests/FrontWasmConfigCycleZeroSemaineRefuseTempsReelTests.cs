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
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@erreur</c>) — VOLET runtime de la garde N ≥ 1 :
/// depuis l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent
/// a déjà défini un cycle de fond de 2 semaines (index 0 → <c>parent-a</c> « Alice » bleu, index 1 →
/// <c>parent-b</c> « Bruno » orange) ; il tente ensuite d'enregistrer un cycle de <b>zéro semaine</b> (N = 0).
/// L'édition est <b>refusée</b> avec le message métier « le cycle doit compter au moins une semaine » affiché
/// à l'écran, et le <b>cycle de 2 semaines précédent reste inchangé</b> — la grille continue d'afficher
/// l'alternance A/B (ISO 27 impaire = Bruno orange, ISO 28 paire = Alice bleu).
///
/// <para>Différence avec le Sc.8 (service injoignable) : ici l'échec est un <b>refus métier 4xx</b> (le handler
/// <c>DefinirCycle</c> s'exécute et oppose la garde N &lt; 1, <c>Result.Echec</c>), PAS un échec de transport.
/// Le motif affiché est donc le motif <b>métier</b> propagé par le canal (<c>Results.BadRequest(string)</c>),
/// jamais un message technique.</para>
///
/// <para>Anti « vert qui ment » : tout le chemin transite réellement — le POST <c>/api/canal/definir-cycle</c>
/// du cycle N = 0 atteint le handler réel via le canal HTTP de l'API distante, le refus métier revient en
/// corps JSON, et l'observable cardinal « le cycle précédent reste inchangé » est vérifié sur le <b>store
/// cycle réel</b> de l'API (<see cref="IReferentielCycleDeFond.CycleCourant"/> porte encore N = 2 et résout
/// l'alternance A/B) ET sur la <b>vraie</b> grille (<see cref="PlanningPartage"/>) câblée à la même API.
/// Aucune règle métier dans l'UI : la garde N ≥ 1 reste portée par le handler ; l'UI ne fait que propager
/// et afficher le refus. Un bUnit à doublure de transport ne prouverait ni la DI réelle, ni le chemin HTTP
/// d'écriture refusé, ni l'absence d'effet de bord sur le store.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigCycleZeroSemaineRefuseTempsReelTests : TestContext
{
    private const string MessageZeroSemaine = "le cycle doit compter au moins une semaine";

    [Fact]
    public void Should_Refuser_l_edition_avec_le_message_le_cycle_doit_compter_au_moins_une_semaine_et_conserver_le_cycle_precedent_When_un_parent_tente_d_enregistrer_un_cycle_de_zero_semaine()
    {
        // Given — l'API distante réelle (store vierge ; référentiel + palette réels : parent-a « Alice »
        // bleu, parent-b « Bruno » orange). L'écran de configuration ET la grille sont câblés à cette MÊME
        // API distante (même store cycle singleton). Tous les services sont enregistrés AVANT tout rendu.
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

        // … un cycle de fond de 2 semaines est d'abord défini depuis l'écran (le « cycle précédent ») :
        // index 0 → parent-a, index 1 → parent-b. L'écriture transite par le canal HTTP réel et aboutit.
        // … le cycle de fond est désormais sous l'onglet « Période de garde » (Sc.2, s20) : on l'active.
        GrilleRuntimeHarness.AllerOngletPeriodeGarde(config);
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("2"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-0']").Change("parent-a"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-b"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));

        // … le store cycle réel porte bien le cycle N = 2 (baseline du « cycle précédent »).
        var storeCycle = api.Services.GetRequiredService<IReferentielCycleDeFond>();
        Assert.Equal(2, storeCycle.CycleCourant()!.NombreSemaines);

        // When — le parent RÉ-OUVRE la modal d'édition (le succès précédent l'avait fermée, refonte s33 Sc.10)
        // et tente d'enregistrer un cycle de ZÉRO semaine : il porte le nombre de semaines à 0 (le min HTML est
        // contournable côté navigateur — le serveur est le gardien) et valide.
        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("0"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());

        // Then — l'édition est refusée avec le message métier affiché à l'écran (motif propagé par le canal,
        // jamais un message technique) ; la confirmation précédente a disparu.
        var alerte = config.WaitForElement("[data-testid='motif-echec-cycle']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageZeroSemaine, alerte.TextContent.Trim());
        Assert.Empty(config.FindAll("[data-testid='confirmation-cycle']"));

        // … le cycle de 2 semaines précédent reste inchangé dans le store réel : aucun écrasement par N = 0,
        // le cycle résout encore l'alternance A/B (ISO 28 paire → parent-a, ISO 27 impaire → parent-b).
        var cycle = storeCycle.CycleCourant();
        Assert.NotNull(cycle);
        Assert.Equal(2, cycle!.NombreSemaines);
        Assert.Equal("parent-a", cycle.ResponsableDeFond(new DateOnly(2026, 7, 6)));   // ISO 28 (paire)
        Assert.Equal("parent-b", cycle.ResponsableDeFond(new DateOnly(2026, 6, 29)));  // ISO 27 (impaire)

        // … et la vraie grille câblée à la MÊME API continue d'afficher l'alternance A/B inchangée.
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));

        var caseIso27 = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Equal("Bruno", caseIso27.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseIso27.GetAttribute("data-couleur"));

        var caseIso28 = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
        Assert.Equal("Alice", caseIso28.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseIso28.GetAttribute("data-couleur"));
    }
}
