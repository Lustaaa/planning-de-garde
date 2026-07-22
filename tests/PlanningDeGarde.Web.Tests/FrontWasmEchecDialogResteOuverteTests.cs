using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ scénario IHM, <c>@erreur</c>, Scenario Outline) — règle 28,
/// décision CP : refus du domaine ET API injoignable partagent <b>un seul observable</b>. Une commande de
/// pose qui <b>n'aboutit pas</b> (depuis la dialog ouverte par le menu d'une case) laisse la dialog
/// <b>ouverte</b>, affiche un <b>message dans la dialog</b>, <b>conserve la saisie</b> à resoumettre, et
/// laisse la <b>grille inchangée</b> (aucune écriture aboutie au store distant).
///
/// Anti « vert qui ment » : le cas <b>API injoignable</b> est prouvé sur <b>transport réellement coupé</b>
/// (le POST poser-slot lève une HttpRequestException via <see cref="GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable"/>,
/// la lecture de la grille passant normalement), pas une doublure de statut 4xx. L'absence d'écriture est
/// vérifiée sur le <b>store réel</b> de l'API distante.
/// </summary>
public sealed class FrontWasmEchecDialogResteOuverteTests : TestContext
{
    // Vendredi 19/06/2026 : la case d'où l'on pose. Référence au 19/06 → fenêtre couvrant ce jour.
    private static readonly DateTime Vendredi_19_06_2026 = new(2026, 6, 19);

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_message_et_saisie_conservee_et_grille_inchangee_When_l_API_est_injoignable_au_moment_de_la_pose()
    {
        // Given — la grille réellement câblée, mais dont l'écriture poser-slot subit un échec de
        // transport déterministe (la lecture initiale de la grille passe normalement).
        using var api = new ApiDistanteFactory();
        var clientEcritureCoupee = GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "api/slots");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Vendredi_19_06_2026, clientEcritureCoupee);

        // When — un Parent ouvre la dialog depuis la case du 19/06, choisit « école » et valide ;
        // le transport d'écriture est coupé.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "19/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — observable unique (règle 28) : dialog ouverte + message dans la dialog + saisie
        // conservée (« école ») + grille inchangée.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));        // dialog ouverte
                var message = grille.Find("[data-testid='dialog-poser-slot'] [data-testid='motif-echec']");
                Assert.False(string.IsNullOrWhiteSpace(message.TextContent));                 // message dans la dialog
                Assert.Equal("école", grille.Find("[data-testid='champ-lieu']").GetAttribute("value")); // saisie conservée
                Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "19/06").QuerySelectorAll("[data-testid='slot-case']")); // grille inchangée
            },
            TimeSpan.FromSeconds(10));

        // … et aucune écriture n'a abouti au store distant pour le 19/06 (rempart anti vert-qui-ment).
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        Assert.Empty(grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 19)).Slots);
    }

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_message_et_grille_inchangee_When_la_pose_est_refusee_par_le_domaine()
    {
        // Given — la grille réellement câblée (transport complet).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Vendredi_19_06_2026);

        // When — un Parent ouvre la dialog depuis la case du 19/06 et valide sans choisir de lieu
        // valide (lieu vide → le domaine refuse : « le lieu visé n'existe pas »).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "19/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — même observable : dialog ouverte + message dans la dialog + grille inchangée.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                var message = grille.Find("[data-testid='dialog-poser-slot'] [data-testid='motif-echec']");
                Assert.False(string.IsNullOrWhiteSpace(message.TextContent));
                Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "19/06").QuerySelectorAll("[data-testid='slot-case']"));
            },
            TimeSpan.FromSeconds(10));

        // … et aucune écriture aboutie au store distant pour le 19/06.
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        Assert.Empty(grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 19)).Slots);
    }
}
