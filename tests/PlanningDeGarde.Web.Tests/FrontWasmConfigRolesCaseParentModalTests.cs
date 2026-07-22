using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 36 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE) : la modal
/// d'édition d'un rôle (patron crayon → modal, s33) porte une case « rôle parent » reflétant l'état COURANT
/// du flag du rôle (source de vérité, s36 B1). (Dé)cocher puis « Enregistrer » émet la commande
/// <c>MarquerRoleParent</c> (POST /api/canal/marquer-role-parent) via le canal HTTP réel — le flag est
/// PERSISTÉ (store réel). Sous identité effective NON-Parent, ni crayon ni case ne sont actionnables
/// (Parent-gated, lecture seule). La convergence TEMPS RÉEL SignalR d'une bascule inter-écran est prouvée
/// par Sc.7 (via le sélecteur des parents, observable de table qui suit sans re-ouverture de modal) ;
/// l'endpoint marquer-role-parent diffuse (INotificateurPlanning) sur écriture aboutie (règle 27).
/// </summary>
public sealed class FrontWasmConfigRolesCaseParentModalTests : TestContext
{
    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(Bunit.TestContext ctx, ApiDistanteFactory api, SessionPlanning? session = null)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(session ?? new SessionPlanning());
        var config = ctx.RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count > 0, TimeSpan.FromSeconds(10));
        return config;
    }

    private void OuvrirEditionRole(IRenderedComponent<ConfigurationFoyer> config, string roleId)
    {
        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-role']")
            .Single(b => b.GetAttribute("data-role-id") == roleId).Click());
        config.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void La_case_reflete_le_flag_courant_du_role_Papa_coche_Nounou_decoche()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(this, api);

        // « Papa » est marqué parent au seed → case COCHÉE ; « Nounou » non marqué → case DÉCOCHÉE.
        OuvrirEditionRole(config, "role-papa");
        Assert.True(config.Find("[data-testid='checkbox-role-parent']").HasAttribute("checked"));
        this.SurDispatcher(() => config.Find("[data-testid='dialog-role-annuler']").Click());

        OuvrirEditionRole(config, "role-nounou");
        Assert.False(config.Find("[data-testid='checkbox-role-parent']").HasAttribute("checked"));
    }

    [Fact]
    public void Cocher_la_case_sur_Nounou_puis_Enregistrer_persiste_le_flag_parent_dans_le_store()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(this, api);
        Assert.False(api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles()
            .Single(r => r.Id == "role-nounou").EstRoleParent); // baseline

        OuvrirEditionRole(config, "role-nounou");
        this.SurDispatcher(() => config.Find("[data-testid='checkbox-role-parent']").Change(true));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Le flag est persisté (POST marquer-role-parent réel), modal fermée.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
                Assert.True(api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles()
                    .Single(r => r.Id == "role-nounou").EstRoleParent);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Decocher_la_case_sur_Papa_puis_Enregistrer_retire_le_flag_parent_dans_le_store()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(this, api);

        OuvrirEditionRole(config, "role-papa");
        this.SurDispatcher(() => config.Find("[data-testid='checkbox-role-parent']").Change(false));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
                Assert.False(api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles()
                    .Single(r => r.Id == "role-papa").EstRoleParent);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Sous_identite_non_Parent_ni_crayon_ni_case_actionnables_lecture_seule()
    {
        using var api = new ApiDistanteFactory();
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(this, api, session);
        Assert.False(session.EstParent);

        // Table visible en lecture (« Papa » présent), mais aucun crayon → aucune modal → aucune case.
        Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Papa");
        Assert.Empty(config.FindAll("[data-testid='crayon-role']"));
        Assert.Empty(config.FindAll("[data-testid='checkbox-role-parent']"));

        // Contrôle positif (anti faux-vert) : sous Parent, crayon et case redeviennent atteignables.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-role']"));
        OuvrirEditionRole(config, "role-papa");
        Assert.NotEmpty(config.FindAll("[data-testid='checkbox-role-parent']"));
    }
}
