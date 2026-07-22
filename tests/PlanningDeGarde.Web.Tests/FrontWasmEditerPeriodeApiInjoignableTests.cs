using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.10 (🖥️ IHM, <c>@erreur</c> — règle 28). Une édition dont le
/// <c>POST /api/canal/editer-periode</c> n'atteint pas l'API distante (échec de transport déterministe via
/// <see cref="GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable"/>) doit : afficher un message d'échec
/// clair <b>dans</b> la dialog, <b>laisser la dialog ouverte</b>, ne <b>rien</b> appliquer (la période reste
/// « Nina la nounou », la case et la légende inchangées), et n'effectuer <b>aucune mise en file ni rejeu</b>.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Planning.PlanningPartage"/> (front WASM) câblée à
/// l'<b>API distante réelle</b> ; seule l'écriture d'édition est coupée au transport (la lecture des
/// périodes et des acteurs de la dialog transite normalement, comme en condition réelle). Anti « vert qui
/// ment » : la période est observée toujours affectée à « nounou » dans le store distant après l'échec.
/// </summary>
public sealed class FrontWasmEditerPeriodeApiInjoignableTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Garder_la_dialog_ouverte_avec_message_d_echec_et_ne_rien_appliquer_When_le_post_d_edition_n_atteint_pas_l_api()
    {
        // Given — la grille câblée à l'API distante (mais dont le POST d'édition est injoignable au
        // transport) ; une période « Nina la nounou » attribue le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);
        var client = GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "/periodes/");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026, client);

        // … baseline : la case du 16/06 porte « Nina la nounou ».
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // When — j'ouvre la dialog d'édition (la LECTURE des périodes/acteurs transite normalement).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-editer-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-editer-periode']"));
            },
            TimeSpan.FromSeconds(10));
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='periode-editable']"),
                l => l.TextContent.Contains("Nina la nounou")),
            TimeSpan.FromSeconds(10));

        // … j'ouvre le formulaire, réaffecte à « Parent A » et j'enregistre : le POST échoue (transport).
        this.SurDispatcher(() => grille.FindAll("[data-testid='periode-editable']")
            .Single(l => l.TextContent.Contains("Nina la nounou"))
            .QuerySelector("[data-testid='bouton-editer-periode']")!.Click());
        var champResponsable = grille.WaitForElement("[data-testid='champ-responsable-edition']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => champResponsable.Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='bouton-enregistrer-edition']").Click());

        // Then — un message d'échec clair s'affiche DANS la dialog, qui reste OUVERTE ; aucun accusé de
        // succès ; la case du 16/06 reste « Nina la nounou ».
        grille.WaitForAssertion(
            () =>
            {
                var dialog = grille.Find("[data-testid='dialog-editer-periode']");
                Assert.Equal(MessagesEcriture.ServiceInjoignable,
                    dialog.QuerySelector("[data-testid='motif-echec']")!.TextContent.Trim());
                Assert.Empty(grille.FindAll("[data-testid='accuse-periode-modifiee']"));
                Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
                    .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … la dialog est toujours présente, la période de Nina toujours affectée à « nounou » dans le store
        // distant (rien appliqué, aucune mise en file ni rejeu : le code ne réémet jamais), légende conservée.
        Assert.NotEmpty(grille.FindAll("[data-testid='dialog-editer-periode']"));
        Assert.Contains(grille.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");

        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        var relue = Assert.Single(periodesDuJour.Lister(new DateOnly(2026, 6, 16)));
        Assert.Equal("nounou", relue.ResponsableId);
    }
}
