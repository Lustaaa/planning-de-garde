using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 22 — Sc.8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le <b>gating</b> sur l'identité effective
/// (règle 9, durcissement config s14, par onglet s20) couvre les affordances d'identité ajoutées ce sprint.
/// Sous une identité effective <b>« Invité »</b> (non Parent/Admin) sur l'onglet « Acteurs » de l'écran de
/// configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel) :
/// aucune affordance de <b>création/association de compte</b> (champ email + bouton) ni de <b>désignation de
/// l'admin du foyer</b> n'est proposée. La <b>lecture</b> reste visible (liste des acteurs). Le durcissement
/// acquis (édition / ajout / suppression d'acteur, gestion des rôles) n'a pas régressé.
///
/// <para>Contrôle positif (anti faux-vert) : sous l'identité Parent, la création de compte et la désignation
/// d'admin REDEVIENNENT proposées — sinon leur absence sous « Invité » serait un faux vert (surfaces cassées
/// pour tous). Déterministe (aucun hub SignalR câblé) : le gating se lit sur l'identité effective.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigGatingInviteCompteAdminTempsReelTests : TestContext
{
    [Fact]
    public void Un_Invite_ne_peut_ni_creer_un_compte_ni_designer_l_admin_du_foyer()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle, sous « Invité »
        // (EstParent = false).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstParent); // garde-fou : un Invité n'a pas le droit d'écrire

        // Then (onglet « Acteurs ») — ni création de compte (champ email + bouton) ni désignation d'admin ne
        // sont proposées ; la lecture (liste des acteurs) reste visible.
        Assert.Empty(config.FindAll("[data-testid='champ-email-compte']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-creer-compte']"));
        // Swap s33 Sc.4 : la désignation d'admin est le TOGGLE de la modal — inatteignable sous Invité (ni crayon ni modal).
        Assert.Empty(config.FindAll("[data-testid='toggle-admin']"));
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));

        // Non-régression durcissement s14/s20 — écritures d'acteur et gestion des rôles restent gatées.
        Assert.Empty(config.FindAll("[data-testid='champ-nom']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer']"));
        Assert.Empty(config.FindAll("[data-testid='champ-libelle-role']"));

        // Contrôle positif (anti faux-vert) — sous l'identité Parent, le crayon réapparaît (refonte s32) et,
        // dans la modal ouverte, la création de compte ET la désignation d'admin REDEVIENNENT proposées :
        // preuve que le gating est bien le discriminant.
        session.Role = RoleAuteur.Parent;
        config.Render();
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        Assert.NotEmpty(config.FindAll("[data-testid='champ-email-compte']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-creer-compte']"));
        Assert.NotEmpty(config.FindAll("[data-testid='toggle-admin']"));
    }
}
