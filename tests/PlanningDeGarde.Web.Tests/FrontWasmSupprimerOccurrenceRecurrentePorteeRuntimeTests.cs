using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 54 — S10, révisé passe architecte post-s54 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la
/// GRILLE, <b>cliquer l'activité récurrente</b> ouvre la <b>dialog d'édition de la série</b> (décision PO),
/// qui porte la suppression avec PORTÉE « cette occurrence » OU « toute la série » ; le back applique le
/// chemin correspondant (exception d'occurrence S9 vs suppression de la série). Plus de corbeille sur la
/// grille. Grille <see cref="Web.Components.Planning.PlanningPartage"/> réellement câblée à l'API distante
/// réelle (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel). Anti vert-qui-ment : l'effet est
/// observé sur le store réel via le port (l'exception persistée / la série retirée), jamais une mutation
/// locale de la grille.
/// </summary>
public sealed class FrontWasmSupprimerOccurrenceRecurrentePorteeRuntimeTests : TestContext
{
    // Lundi 22/06/2026 : référence → fenêtre couvrant plusieurs lundis (22/06, 29/06, 06/07…).
    private static readonly DateTime Lundi_22_06_2026 = new(2026, 6, 22);

    private static bool CaseContient(AngleSharp.Dom.IElement caseJour, string libelle)
        => caseJour.QuerySelectorAll("[data-testid='slot-case']")
            .Any(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == libelle);

    private static void SemerRecurrentLundi(ApiDistanteFactory api)
        => api.Services.GetRequiredService<ISlotRecurrentRepository>().Enregistrer(
            SlotRecurrent.Poser(GrilleRuntimeHarness.EnfantParDefaut, "École", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);

    private IRenderedComponent<Web.Components.Planning.PlanningPartage> OuvrirDialogSerieSur22_06(ApiDistanteFactory api)
    {
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_22_06_2026);
        grille.WaitForAssertion(
            () => Assert.True(CaseContient(GrilleRuntimeHarness.CaseDuJour(grille, "22/06"), "École")),
            TimeSpan.FromSeconds(10));

        // Clic sur l'activité récurrente « École » du lundi 22/06 → dialog d'édition de la série (post-s54).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "22/06")
            .QuerySelectorAll("[data-testid='slot-case']")
            .Single(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == "École")
            .Click());
        // La dialog partagée s'ouvre (chargement lieux + série) et offre la suppression avec portée.
        grille.WaitForAssertion(
            () => Assert.NotEmpty(grille.FindAll("[data-testid='scope-toute-la-serie']")), TimeSpan.FromSeconds(10));
        return grille;
    }

    [Fact]
    public void Should_retirer_la_seule_occurrence_du_22_06_et_garder_les_autres_When_on_choisit_cette_occurrence()
    {
        using var api = new ApiDistanteFactory();
        SemerRecurrentLundi(api);
        var grille = OuvrirDialogSerieSur22_06(api);

        // When — on choisit « cette occurrence ».
        this.SurDispatcher(() => grille.Find("[data-testid='scope-cette-occurrence']").Click());

        // Then — le lundi 22/06 ne rend plus « École », mais le lundi suivant 29/06 le rend encore (série continue).
        grille.WaitForAssertion(
            () =>
            {
                Assert.False(CaseContient(GrilleRuntimeHarness.CaseDuJour(grille, "22/06"), "École"));
                Assert.True(CaseContient(GrilleRuntimeHarness.CaseDuJour(grille, "29/06"), "École"));
            },
            TimeSpan.FromSeconds(10));

        // … et l'exception est persistée sur la série (store réel) — la série d'origine demeure.
        var snapshot = Assert.Single(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.Contains(new PlageExclusion(new DateOnly(2026, 6, 22), new DateOnly(2026, 6, 22)), snapshot.Exclusions);
    }

    [Fact]
    public void Should_retirer_toute_la_serie_When_on_choisit_toute_la_serie()
    {
        using var api = new ApiDistanteFactory();
        SemerRecurrentLundi(api);
        var grille = OuvrirDialogSerieSur22_06(api);

        // When — on choisit « toute la série ».
        this.SurDispatcher(() => grille.Find("[data-testid='scope-toute-la-serie']").Click());

        // Then — plus aucune occurrence « École » (ni 22/06 ni 29/06) : la série a disparu.
        grille.WaitForAssertion(
            () =>
            {
                Assert.False(CaseContient(GrilleRuntimeHarness.CaseDuJour(grille, "22/06"), "École"));
                Assert.False(CaseContient(GrilleRuntimeHarness.CaseDuJour(grille, "29/06"), "École"));
            },
            TimeSpan.FromSeconds(10));

        // … et la série a réellement été retirée du store réel (rempart anti vert-qui-ment).
        Assert.Empty(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
    }
}
