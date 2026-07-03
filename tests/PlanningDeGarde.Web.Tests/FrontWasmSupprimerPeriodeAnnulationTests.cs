using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@limite</c> — annulation). Ouvrir la dialog de
/// suppression puis la <b>fermer sans supprimer</b> ne doit émettre <b>aucune</b> commande d'écriture :
/// la période reste présente et la case reste inchangée. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel). Le bouton « Fermer » (<c>OnAnnule</c> → <c>FermerDialog</c>)
/// ne passe jamais par le canal d'écriture : preuve observable = aucun accusé « Période supprimée », la
/// période toujours relue depuis le store distant, la case toujours « Nina la nounou ».
///
/// Anti « vert qui ment » : le baseline « Nina la nounou » est asserté avant ; si la fermeture émettait une
/// suppression, la case retomberait (neutre/fond) et la période disparaîtrait du store → rouge.
/// </summary>
public sealed class FrontWasmSupprimerPeriodeAnnulationTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Laisser_la_periode_et_la_case_du_16_06_inchangees_When_un_parent_ferme_la_dialog_de_suppression_sans_supprimer()
    {
        // Given — la grille câblée à l'API distante, pour un Parent ; une période « Nina la nounou »
        // attribue le mardi 16/06/2026 (surcharge ; aucun fond → la case porte la période).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 porte « Nina la nounou ».
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // When — j'ouvre la dialog de suppression (menu clic-case → « Supprimer une période »).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-supprimer-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste la période de Nina (chargement async, attendu sans re-cliquer la case).
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='periode-supprimable']"),
                l => l.TextContent.Contains("Nina la nounou")),
            TimeSpan.FromSeconds(10));

        // … et je ferme la dialog SANS supprimer (bouton « Fermer »).
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-supprimer-periode'] [data-testid='dialog-annuler']").Click());

        // Then — la dialog est fermée, AUCUNE commande de suppression n'a été émise : aucun accusé
        // « Période supprimée », la case du 16/06 affiche toujours « Nina la nounou ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-supprimer-periode']"));
                Assert.Empty(grille.FindAll("[data-testid='accuse-periode-supprimee']"));
                Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
                    .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … et la période est toujours présente dans le store de l'API distante (rien n'a été supprimé).
        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        Assert.Contains(periodesDuJour.Lister(new DateOnly(2026, 6, 16)), p => p.ResponsableId == "nounou");
    }
}
