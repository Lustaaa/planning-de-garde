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
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@limite</c> — annulation). Ouvrir le formulaire
/// d'édition, <b>modifier le responsable</b>, puis <b>fermer sans enregistrer</b> ne doit émettre
/// <b>aucune</b> commande d'écriture : la période reste « Nina la nounou » et la case reste inchangée. On
/// rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une
/// <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel). Le bouton « Fermer »
/// (<c>OnAnnule</c> → <c>FermerDialog</c>) ne passe jamais par le canal d'écriture : preuve observable =
/// aucun accusé « Période modifiée », la période toujours relue depuis le store distant avec son responsable
/// d'origine, la case toujours « Nina la nounou ».
///
/// CARACTÉRISATION (⚠️ early green ATTENDU) : la fermeture sans écriture est déjà câblée (dialog posée au
/// Sc.7). Ce test verrouille que la modification non enregistrée du formulaire ne fuit JAMAIS jusqu'au store.
/// Anti « vert qui ment » : le baseline « Nina la nounou » est asserté avant ; si la fermeture émettait une
/// édition, la case afficherait « Alice » et le store porterait « parent-a » → rouge.
/// </summary>
public sealed class FrontWasmEditerPeriodeAnnulationTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Laisser_la_periode_et_la_case_du_16_06_inchangees_When_un_parent_modifie_le_responsable_puis_ferme_le_formulaire_sans_enregistrer()
    {
        // Given — la grille câblée à l'API distante, pour un Parent ; une période « Nina la nounou »
        // attribue le mardi 16/06/2026 (surcharge ; aucun fond → la case porte la période).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 porte « Nina la nounou ».
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // When — j'ouvre la dialog d'édition (menu clic-case → « Éditer une période »).
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();
                grille.Find("[data-testid='action-editer-periode']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-editer-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste la période de Nina (chargement async, attendu sans re-cliquer la case).
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='periode-editable']"),
                l => l.TextContent.Contains("Nina la nounou")),
            TimeSpan.FromSeconds(10));

        // … j'ouvre le formulaire pré-rempli et je MODIFIE le responsable (vers « Parent A ») sans enregistrer.
        grille.FindAll("[data-testid='periode-editable']")
            .Single(l => l.TextContent.Contains("Nina la nounou"))
            .QuerySelector("[data-testid='bouton-editer-periode']")!.Click();
        var champResponsable = grille.WaitForElement("[data-testid='champ-responsable-edition']", TimeSpan.FromSeconds(10));
        champResponsable.Change("parent-a");

        // … et je ferme la dialog SANS enregistrer (bouton « Fermer »).
        grille.Find("[data-testid='dialog-editer-periode'] [data-testid='dialog-annuler']").Click();

        // Then — la dialog est fermée, AUCUNE commande d'édition n'a été émise : aucun accusé « Période
        // modifiée », la case du 16/06 affiche toujours « Nina la nounou ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-editer-periode']"));
                Assert.Empty(grille.FindAll("[data-testid='accuse-periode-modifiee']"));
                Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
                    .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … et la période est toujours présente dans le store distant avec son responsable d'origine
        // (« nounou » inchangé : la modification du formulaire n'a jamais transité par le canal d'écriture).
        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        var relue = Assert.Single(periodesDuJour.Lister(new DateOnly(2026, 6, 16)));
        Assert.Equal("nounou", relue.ResponsableId);
    }
}
