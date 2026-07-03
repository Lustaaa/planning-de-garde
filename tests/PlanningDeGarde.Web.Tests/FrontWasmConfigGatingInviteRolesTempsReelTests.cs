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
/// Sprint 21 — Sc.9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le <b>gating</b> sur l'identité effective
/// (règle 9, durcissement config s14, par onglet s20) couvre la gestion des rôles ajoutée ce sprint. Sous
/// une identité effective <b>« Invité »</b> (non Parent/Admin) sur l'onglet « Acteurs » de l'écran de
/// configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel) :
/// aucune action de gestion du référentiel de rôles n'est proposée — ni créer (champ libellé + bouton),
/// ni renommer, ni supprimer un rôle — et aucun sélecteur d'affectation de rôle à un acteur n'est proposé.
/// La <b>lecture</b> reste visible (liste des acteurs). Le durcissement acquis (édition / ajout / suppression
/// d'acteur) n'a pas régressé.
///
/// <para>Contrôle positif (anti faux-vert) : sous l'identité Parent, ces actions de gestion des rôles et le
/// sélecteur d'affectation REDEVIENNENT proposés — sinon leur absence sous « Invité » serait un faux vert
/// (surfaces cassées pour tous). Déterministe (aucun hub SignalR câblé) : le gating se lit sur l'identité
/// effective, jamais un effet de temps réel.</para>
/// </summary>
public sealed class FrontWasmConfigGatingInviteRolesTempsReelTests : TestContext
{
    [Fact]
    public void Un_Invite_ne_peut_ni_gerer_le_referentiel_de_roles_ni_affecter_un_role_a_un_acteur()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle, sous « Invité »
        // (EstParent = false). On sème un rôle dans le référentiel réel pour que le sélecteur d'affectation
        // aurait de quoi s'afficher s'il n'était pas gaté (anti faux-vert par référentiel vide).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstParent); // garde-fou : un Invité n'a pas le droit d'écrire

        // Then (onglet « Acteurs ») — aucune action de gestion des rôles (créer / renommer / supprimer) ni
        // aucun sélecteur d'affectation de rôle n'est proposé ; la lecture (liste des acteurs) reste visible.
        Assert.Empty(config.FindAll("[data-testid='champ-libelle-role']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-renommer-role']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer-role']"));
        Assert.Empty(config.FindAll("[data-testid='selecteur-role-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));

        // Non-régression durcissement s14/s20 — les écritures d'acteur restent gatées elles aussi.
        Assert.Empty(config.FindAll("[data-testid='champ-nom']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer']"));

        // Contrôle positif (anti faux-vert) — sous l'identité Parent, la gestion des rôles et le sélecteur
        // d'affectation REDEVIENNENT proposés : preuve que le gating est bien le discriminant.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='champ-libelle-role']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-renommer-role']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-supprimer-role']"));
        Assert.NotEmpty(config.FindAll("[data-testid='selecteur-role-acteur']"));
    }
}
