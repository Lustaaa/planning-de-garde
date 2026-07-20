using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.11 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : invariants de la modal Cycle (patron
/// s33 Sc.10). Sur l'écran réellement câblé (API distante réelle, store réel, DI réelle) : (1) sous
/// identité EFFECTIVE non-Parent (Invité), le TABLEAU des cycles reste VISIBLE en lecture seule, sans
/// crayon « Éditer le cycle », aucune modal atteignable ; (2) une valeur refusée par le domaine (zéro
/// semaine) ou une API injoignable à l'enregistrement laisse la modal OUVERTE, le motif DEDANS, la saisie
/// (N + affectations) CONSERVÉE, le tableau INCHANGÉ. La convergence SignalR deux écrans est prouvée par le
/// test frère <c>FrontWasmGrilleDeuxEcransCycleDerniereEcritureGagneTempsReel</c> (migré vers la modal en Sc.10).
/// </summary>
public sealed class FrontWasmConfigCycleInvariantsModalTests : TestContext
{
    private static void SemerCycle(ApiDistanteFactory api)
        => api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }), GrilleRuntimeHarness.EnfantParDefaut);

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api, SessionPlanning? session = null, System.Net.Http.HttpClient? client = null)
    {
        Services.AddSingleton(client ?? GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session ?? new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Sous_identite_Invite_le_tableau_des_cycles_reste_visible_en_lecture_seule_sans_crayon_ni_modal()
    {
        // Given — un cycle déclaré ; écran sous identité effective « Invité ».
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(api, session);
        Assert.False(session.EstParent);

        // Then — le tableau des cycles reste VISIBLE en lecture (deux semaines déclarées), sans crayon ni modal.
        config.WaitForAssertion(
            () => Assert.Equal(2, config.FindAll("[data-testid='cycle-foyer']").Count),
            TimeSpan.FromSeconds(10));
        Assert.Empty(config.FindAll("[data-testid='crayon-cycle']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-cycle']"));

        // Contrôle positif (anti faux-vert) — sous Parent, le crayon « Éditer le cycle » REDEVIENT rendu.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-cycle']"));
    }

    [Fact]
    public void Un_refus_zero_semaine_laisse_la_modal_ouverte_avec_le_motif_la_saisie_conservee_et_le_tableau_inchange()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='cycle-foyer']").Count == 2, TimeSpan.FromSeconds(10));

        // When — j'ouvre la modal (pré-remplie N=2), porte N à 0 (refusé par le domaine) et enregistre.
        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("0"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());

        // Then — la modal RESTE OUVERTE, le motif est DEDANS, la saisie N=0 CONSERVÉE dans le champ (les
        // selects de semaine ne sont pas rendus à N=0, mais les affectations restent en état — conservation
        // prouvée par le volet « API injoignable » qui garde N=2), et le tableau reste INCHANGÉ (toujours les
        // deux semaines déclarées d'origine — aucune écriture partielle).
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-cycle']");
                Assert.NotNull(modal.QuerySelector("[data-testid='motif-echec-cycle']"));
                Assert.Equal("0", modal.QuerySelector("[data-testid='champ-nombre-semaines']")!.GetAttribute("value"));
                Assert.Equal(2, config.FindAll("[data-testid='cycle-foyer']").Count);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Une_API_injoignable_a_l_enregistrement_du_cycle_laisse_la_modal_ouverte_avec_le_motif_et_la_saisie_conservee()
    {
        // Given — un cycle déclaré ; le canal /definir-cycle subit un échec de transport déterministe.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var config = RendreConfig(api, client: GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "definir-cycle"));
        config.WaitForState(() => config.FindAll("[data-testid='cycle-foyer']").Count == 2, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-a"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());

        // Then — la modal RESTE OUVERTE, motif « service injoignable » dedans, saisie conservée, tableau inchangé.
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-cycle']");
                Assert.Equal(MessagesEcriture.ServiceInjoignable,
                    modal.QuerySelector("[data-testid='motif-echec-cycle']")!.TextContent.Trim());
                Assert.Equal("parent-a", modal.QuerySelector("[data-testid='champ-cycle-index-1']")!.GetAttribute("value"));
                Assert.Equal(2, config.FindAll("[data-testid='cycle-foyer']").Count);
            },
            TimeSpan.FromSeconds(10));
    }
}
