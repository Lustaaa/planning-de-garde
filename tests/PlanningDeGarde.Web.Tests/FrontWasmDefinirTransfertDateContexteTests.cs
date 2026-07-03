using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ scénario IHM, <c>@limite</c> — palier 7, 3ᵉ dialog) :
/// <b>caractérisation early-green</b> de l'ancrage de la date de contexte (règle 17 composée). La date de
/// la <b>case cliquée prime</b> sur le défaut horloge « aujourd'hui ». On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel), avec « aujourd'hui » figé au
/// <b>lundi 15/06/2026</b> (<see cref="DateTimeProviderFige"/>), puis on ouvre la dialog depuis la case du
/// <b>jeudi 25/06/2026</b> (différente d'aujourd'hui pour que la contradiction soit non-vacuous).
///
/// Anti « vert qui ment » : si la dialog retombait sur le défaut horloge, le champ date porterait le
/// 15/06 et le transfert persisté dans le store distant tomberait au 15/06 → rouge. La date de contexte
/// est observée deux fois : (1) pré-remplissage du champ date à l'ouverture ; (2) date réellement
/// persistée dans le store de l'API distante après validation (pipeline complet, pas une doublure).
/// </summary>
public sealed class FrontWasmDefinirTransfertDateContexteTests : TestContext
{
    // « Aujourd'hui » figé au lundi 15/06/2026 (début de la fenêtre de 5 semaines) — DIFFÉRENT de la case.
    private static readonly DateTime Lundi_15_06_2026 = new(2026, 6, 15);

    [Fact]
    public void Should_Pre_remplir_la_date_du_transfert_sur_le_jeudi_25_06_2026_et_non_sur_aujourd_hui_le_15_06_2026_When_un_parent_ouvre_la_dialog_depuis_la_case_du_jeudi_25_06_2026()
    {
        // Given — la grille réellement câblée à l'API distante (store réel vierge), « aujourd'hui » figé au
        // lundi 15/06/2026 ; la fenêtre couvre le jeudi 25/06/2026.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_15_06_2026);

        // When — un Parent clique la case du jeudi 25/06 → menu d'actions → 3ᵉ entrée « Définir un
        // transfert » → la dialog s'ouvre. Idempotent sous WaitForAssertion (re-renders async du hub).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "25/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-definir-transfert']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        // Then (1) — la dialog s'ouvre avec la date PRÉ-REMPLIE sur le jeudi 25/06/2026 (date de la case),
        // PAS sur le lundi 15/06/2026 (défaut horloge). Le champ date InputDate rend "yyyy-MM-dd".
        var champDate = grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-date']");
        Assert.Equal("2026-06-25", champDate.GetAttribute("value"));

        // … et le transfert validé sans toucher la date pré-remplie transite réellement jusqu'au store
        // distant en portant la date de contexte (rempart anti vert-qui-ment : pipeline complet).
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-recupere']").Change("parent-b"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-heure']").Change("08:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] form").Submit());

        grille.WaitForAssertion(
            () => Assert.NotEmpty(grille.FindAll("[data-testid='accuse-transfert-defini']")),
            TimeSpan.FromSeconds(10));

        // Then (2) — la date persistée dans le store de l'API distante est le 25/06/2026 (date de la case),
        // jamais le 15/06/2026 (défaut horloge).
        using var scope = api.Services.CreateScope();
        var transferts = scope.ServiceProvider.GetRequiredService<ITransfertRepository>();
        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal(new DateOnly(2026, 6, 25), DateOnly.FromDateTime(transfert.Date));
    }
}
