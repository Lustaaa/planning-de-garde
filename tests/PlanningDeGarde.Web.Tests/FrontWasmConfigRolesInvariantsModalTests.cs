using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : invariants de la modal Rôles (patron
/// s33 Sc.8). Sur l'écran réellement câblé (API distante réelle, store réel, DI réelle) : (1) un libellé
/// refusé par le domaine (doublon) ou une API injoignable à l'enregistrement laisse la modal OUVERTE, le
/// motif DEDANS, la saisie CONSERVÉE, la table INCHANGÉE ; (2) sous identité EFFECTIVE non-Parent (Invité),
/// l'onglet Rôles reste en LECTURE SEULE (table visible), sans crayon ni « Ajouter », aucune modal
/// atteignable. La convergence SignalR deux écrans (édition/ajout/suppression) est prouvée par le test
/// frère <c>FrontWasmConfigRolesDeuxEcransConvergenceTempsReelTests</c> (migré vers la modal en Sc.8).
/// </summary>
public sealed class FrontWasmConfigRolesInvariantsModalTests : TestContext
{
    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

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
    public void Un_libelle_doublon_laisse_la_modal_ouverte_avec_le_motif_dedans_la_saisie_conservee_et_la_table_inchangee()
    {
        // Given — un rôle « Nounou » existe ; on ouvre la modal d'ajout et on tente de recréer « Nounou ».
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count == 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Nounou"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Then — la modal RESTE OUVERTE, le motif de refus est DEDANS, la saisie « Nounou » conservée, et la
        // table n'a pas gagné de rôle (toujours un seul « Nounou », aucune écriture partielle).
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-role']");
                Assert.NotNull(modal.QuerySelector("[data-testid='motif-echec-role']"));
                Assert.Equal("Nounou", modal.QuerySelector("[data-testid='champ-libelle-role']")!.GetAttribute("value"));
                Assert.Single(config.FindAll("[data-testid='role-foyer']"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Une_API_injoignable_a_l_enregistrement_laisse_la_modal_ouverte_avec_le_motif_et_la_saisie_conservee()
    {
        // Given — le canal /creer-role subit un échec de transport déterministe (les lectures passent).
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api, client: GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "creer-role"));

        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Grand-parent"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Then — la modal RESTE OUVERTE, motif « service injoignable » dedans, saisie « Grand-parent » conservée.
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-role']");
                Assert.Equal(MessagesEcriture.ServiceInjoignable,
                    modal.QuerySelector("[data-testid='motif-echec-role']")!.TextContent.Trim());
                Assert.Equal("Grand-parent", modal.QuerySelector("[data-testid='champ-libelle-role']")!.GetAttribute("value"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Sous_identite_Invite_la_table_des_roles_reste_en_lecture_seule_sans_crayon_ni_ajouter_ni_modal()
    {
        // Given — un rôle est semé ; écran sous identité effective « Invité ».
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(api, session);
        Assert.False(session.EstParent);

        // Then — la table des rôles reste VISIBLE en lecture (« Nounou »), sans crayon, sans « Ajouter », sans modal.
        Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou");
        Assert.Empty(config.FindAll("[data-testid='crayon-role']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-role']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-role']"));

        // Contrôle positif (anti faux-vert) — sous Parent, crayon (par ligne) et « Ajouter » REDEVIENNENT rendus.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-role']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-role']"));
    }
}
