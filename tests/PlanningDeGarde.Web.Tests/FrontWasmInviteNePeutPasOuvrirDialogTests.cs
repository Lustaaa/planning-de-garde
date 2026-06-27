using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ scénario IHM, <c>@erreur</c>) — driver du droit Invité :
/// le déclencheur d'écriture <b>migre vers la case</b> (palier 7) et doit être <b>gaté</b> en
/// consultation seule (règle 9, contexte rôle <c>SessionPlanning</c>). Un <b>Invité</b> qui clique une
/// case <b>n'ouvre aucune dialog</b> ; le déclencheur de la case est <b>désactivé</b> ; la grille reste
/// en lecture seule.
///
/// Non-vacuité (garde-fou CP) : le test porte un <b>contrôle positif</b> — en <b>Parent</b>, le clic
/// ouvre bien le menu d'actions — AVANT le négatif Invité (bascule de rôle via le sélecteur réel). Sans
/// ce contrôle, le test passerait vacuously si le clic était cassé pour tous. Rendu sur la grille
/// réellement câblée à l'API distante (DI réelle du rôle).
/// </summary>
public sealed class FrontWasmInviteNePeutPasOuvrirDialogTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_N_ouvrir_aucune_dialog_d_ecriture_et_garder_la_grille_en_lecture_seule_When_un_invite_clique_une_case_alors_qu_un_parent_le_pourrait()
    {
        // Given — la grille réellement câblée, affichée par défaut pour un Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // CONTRÔLE POSITIF — en Parent, cliquer la case du 16/06 ouvre bien le menu d'actions
        // (le déclencheur d'écriture est actif) ; on referme ensuite le menu.
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Contains("grille-jour-cliquable",
            GrilleRuntimeHarness.CaseDuJour(grille, "16/06").ClassName);
        grille.Find("[data-testid='menu-actions-case']").Click(); // referme le menu (clic hors panneau)
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));

        // When — l'utilisateur bascule en « Invité (consultation seule) » via le sélecteur de rôle réel,
        // puis clique la case du 16/06.
        grille.Find("select.form-select").Change("Invite");
        GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();

        // Then — aucune dialog d'écriture ni menu ne s'ouvre (gating règle 9), le déclencheur de la case
        // est désactivé (plus de classe cliquable), et la consultation seule est signalée.
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
        Assert.DoesNotContain("grille-jour-cliquable",
            GrilleRuntimeHarness.CaseDuJour(grille, "16/06").ClassName);
        Assert.Contains("lecture seule", grille.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
