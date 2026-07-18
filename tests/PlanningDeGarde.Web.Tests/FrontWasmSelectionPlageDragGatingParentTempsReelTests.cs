using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.8 (🖥️ IHM, <c>@gating</c>) — la sélection de plage par drag est <b>Parent-gated à la SOURCE</b>
/// (règle 9, mutualisée avec le menu clic-case) : en mode <b>Invité</b> (non-Parent), un mousedown ne pose PAS
/// d'ancre → le geste est <b>INERTE</b> (aucune surbrillance, aucune dialog, aucun menu), quel que soit le survol
/// ou le relâchement. <b>Seul un Parent</b> arme la sélection (l'ancre est posée, la surbrillance apparaît). Rendu
/// sur la grille <b>réellement câblée</b> à l'API distante (store réel) : le store reste VIDE après le geste inerte.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageDragGatingParentTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void L_invite_ne_peut_pas_selectionner_de_plage_le_drag_est_inerte()
    {
        // Given — grille réelle câblée (store vierge). L'identité effective bascule en Invité (consultation seule).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // When — l'Invité presse J1 (09/06), survole jusqu'à J3 (11/06), puis relâche : le geste complet.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").MouseDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").MouseOver());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").MouseUp());

        // Then — AUCUNE surbrillance de plage, AUCUNE dialog, AUCUN menu (drag inerte) ; le store distant est VIDE.
        Assert.Empty(grille.FindAll("[data-plage-drag='1']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public void Un_parent_arme_bien_la_selection_le_drag_pose_l_ancre_et_surligne()
    {
        // Given — même grille réelle câblée, mais l'utilisateur reste Parent (identité par défaut du harnais).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);

        // When — le Parent presse J1 (09/06) et survole J3 (11/06) : le gate laisse passer, l'ancre est armée.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").MouseDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").MouseOver());

        // Then — la surbrillance de plage apparaît (l'ancre a bien été posée) : seul un Parent sélectionne.
        grille.WaitForAssertion(
            () => Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "10/06").GetAttribute("data-plage-drag")),
            TimeSpan.FromSeconds(10));
    }
}
