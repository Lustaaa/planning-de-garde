using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ scénario IHM, <c>@limite</c> — driver de design du palier 7,
/// règle 17 composée) : l'<b>ancrage case</b> (date de la case cliquée) <b>prime</b> sur le défaut
/// <see cref="IDateTimeProvider"/> « aujourd'hui ». Date de référence figée au <b>lundi 15/06/2026</b> ;
/// on clique la case du <b>jeudi 25/06/2026</b> : la dialog doit s'ouvrir ancrée sur le 25/06 (et non le
/// 15/06). On valide <b>sans toucher la date pré-remplie</b> ; le slot réellement enregistré doit
/// réapparaître dans la case du <b>25/06</b> et <b>aucun</b> dans celle du 15/06.
///
/// Non-vacuité : si la dialog s'ancrait sur « aujourd'hui » (15/06) au lieu de la date de la case, le
/// slot retomberait au 15/06 → l'assertion « slot au 25/06 / rien au 15/06 » échouerait (rouge). On rend
/// la vraie grille câblée à l'API distante réelle (store réel, projection réelle).
/// </summary>
public sealed class FrontWasmPreRemplirDateCaseTests : TestContext
{
    // Aujourd'hui = lundi 15/06/2026 ; la case cliquée = jeudi 25/06/2026 (toutes deux dans la fenêtre).
    private static readonly DateTime Lundi_15_06_2026 = new(2026, 6, 15);

    [Fact]
    public void Should_Pre_remplir_la_saisie_sur_le_jeudi_25_06_2026_et_y_faire_apparaitre_le_slot_When_la_dialog_est_ouverte_depuis_la_case_du_25_06_alors_qu_aujourd_hui_est_le_15_06()
    {
        // Given — la grille réellement câblée, affichée pour un Parent, référence « aujourd'hui » au 15/06.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_15_06_2026);

        // … les deux cases sont visibles et sans slot.
        Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "25/06").QuerySelectorAll("[data-testid='slot-case']"));
        Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "15/06").QuerySelectorAll("[data-testid='slot-case']"));

        // When — un Parent clique la case du jeudi 25/06 → menu → « Poser un slot » → la dialog s'ouvre
        // (ouverture idempotente sous WaitForAssertion : robuste aux re-renders async du hub sous charge).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "25/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit un lieu et valide SANS toucher la date pré-remplie (ancrée sur la date de la case).
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("domicile A"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — la dialog se ferme ET le slot réapparaît dans la case du 25/06 (date de contexte),
        // pas dans celle du 15/06 (« aujourd'hui ») : l'ancrage case a primé sur le défaut horloge.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                Assert.NotEmpty(GrilleRuntimeHarness.CaseDuJour(grille, "25/06").QuerySelectorAll("[data-testid='slot-case']"));
                Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "15/06").QuerySelectorAll("[data-testid='slot-case']"));
            },
            TimeSpan.FromSeconds(10));

        // … et l'écriture est réellement au 25/06 dans le store distant (rempart anti vert-qui-ment).
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        Assert.Single(grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 25)).Slots);
        Assert.Empty(grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 15)).Slots);
    }
}
