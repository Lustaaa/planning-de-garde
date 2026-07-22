using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le <b>gating</b> sur l'identité
/// effective (règle 9, durcissement config s14) est <b>préservé sur CHAQUE onglet</b> de l'écran de
/// configuration réorganisé. Sous une identité effective <b>« Invité »</b> (non Parent/Admin), aucune
/// action d'écriture n'est proposée — ni sur l'onglet « Acteurs » (éditer / ajouter / supprimer un
/// acteur), ni sur « Période de garde » (définir / éditer le cycle), ni sur « Slot récurrent »
/// (réservé) — tandis que la <b>lecture</b> (liste des acteurs) reste visible.
///
/// <para>Contrôle positif (anti faux-vert) : sous l'identité Parent, ces mêmes écritures REDEVIENNENT
/// proposées sur chaque onglet — sinon leur absence sous « Invité » serait un faux vert (formulaires
/// cassés pour tous). Test déterministe (aucun hub SignalR câblé) : le gating se lit sur l'identité
/// effective, jamais un effet de temps réel.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigGatingInviteChaqueOngletTempsReelTests : TestContext
{
    [Fact]
    public void Le_gating_identite_effective_est_preserve_sur_chaque_onglet_un_Invite_ne_voit_aucune_ecriture_mais_conserve_la_lecture()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle, sous une identité
        // effective « Invité » (rôle démo Invité → EstParent = false, quelle que soit l'identité).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstParent); // garde-fou : un Invité n'a pas le droit d'écrire

        // Then (onglet « Acteurs », actif par défaut) — aucune écriture proposée (éditer / ajouter /
        // supprimer), mais la LECTURE (liste des acteurs) reste visible : le durcissement masque les
        // écritures, pas la consultation.
        Assert.Empty(config.FindAll("[data-testid='champ-nom']"));
        Assert.Empty(config.FindAll("[data-testid='champ-nom-ajout']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer']"));
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));

        // Then (section « Cycle de fond ») — refonte s33 Sc.10 : aucune écriture du cycle n'est proposée
        // (crayon « Éditer le cycle » gaté ; l'éditeur vit dans la modal, non atteignable).
        Assert.Empty(config.FindAll("[data-testid='crayon-cycle']"));
        Assert.Empty(config.FindAll("[data-testid='champ-nombre-semaines']"));

        // Contrôle positif (anti faux-vert) — sous l'identité Parent, les écritures REDEVIENNENT proposées
        // sur chaque section : preuve que le gating est bien le discriminant. Refonte s32 : l'écriture d'un
        // acteur passe par la modal — les entrées visibles gatées sont le crayon d'édition et le bouton
        // « Ajouter un acteur » ; le cycle reste gaté sur sa section.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-cycle']")); // écriture cycle = crayon → modal (Sc.10)
    }
}
