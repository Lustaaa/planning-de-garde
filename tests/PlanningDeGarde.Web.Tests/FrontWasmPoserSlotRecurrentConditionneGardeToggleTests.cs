using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sprint 31 — Sc.14 (🖥️ scénario IHM, D1) : le toggle « seulement les
/// jours où l'enfant est chez moi » dans la dialog « Poser un slot » (récurrent) pose un slot récurrent
/// <b>conditionné à la garde</b> (ConditionneGarde=true) portant l'identité du <b>parent courant</b>
/// (PoseurId = identité effective de la session). Sur la grille réellement câblée (front WASM
/// <see cref="Web.Components.Pages.PlanningPartage"/> + API distante réelle, store réel, canal d'écriture
/// HTTP réel), la pose transite jusqu'au store distant avec le conditionnement demandé ; laisser le toggle
/// inactif pose un slot au comportement s29 par défaut (non conditionné).
///
/// Anti « vert qui ment » : si le toggle n'existe pas dans la dialog, si le conditionnement / le poseur ne
/// sont pas transmis de bout en bout (requête → commande → handler → agrégat → store), l'observable sur le
/// store réel reste au défaut (ConditionneGarde=false) → rouge. Un bUnit à doublure de transport ne verrait
/// ni ce câblage distant ni la matérialisation réelle du flag côté store.
/// </summary>
public sealed class FrontWasmPoserSlotRecurrentConditionneGardeToggleTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    private static void OuvrirDialogPoseRecurrenteDepuisSamedi(Bunit.TestContext ctx, IRenderedComponent<Web.Components.Pages.PlanningPartage> grille)
    {
        grille.WaitForAssertion(
            () =>
            {
                ctx.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "04/07").Click());
                ctx.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        ctx.SurDispatcher(() => grille.Find("[data-testid='champ-repeter-hebdo']").Change(true));
        grille.WaitForElement("[data-testid='champ-heure-debut']", TimeSpan.FromSeconds(10));
        ctx.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("piscine"));
        ctx.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut']").Change("11:30"));
        ctx.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin']").Change("12:15"));
    }

    [Fact]
    public void Should_Enregistrer_le_slot_recurrent_conditionne_a_la_garde_du_parent_courant_When_le_toggle_seulement_les_jours_ou_l_enfant_est_chez_moi_est_actif()
    {
        // Given — la grille réellement câblée à l'API distante ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurLieux>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // When — un Parent pose un slot récurrent en ACTIVANT le toggle « seulement les jours où l'enfant
        // est chez moi », puis valide.
        OuvrirDialogPoseRecurrenteDepuisSamedi(this, grille);
        this.SurDispatcher(() => grille.Find("[data-testid='champ-conditionne-garde']").Change(true));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — la dialog se ferme (succès acquitté)…
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']")),
            TimeSpan.FromSeconds(10));

        // … et le slot récurrent a transité jusqu'au store distant CONDITIONNÉ à la garde, portant l'identité
        // du parent courant (identité effective de la session connectée = « configurateur »).
        using var scope = api.Services.CreateScope();
        var enregistre = Assert.Single(scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.True(enregistre.ConditionneGarde);
        Assert.Equal("configurateur", enregistre.PoseurId);
    }

    [Fact]
    public void Should_Enregistrer_le_slot_recurrent_non_conditionne_par_defaut_When_le_toggle_reste_inactif()
    {
        // Given — la grille réellement câblée ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurLieux>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // When — un Parent pose un slot récurrent SANS activer le toggle (comportement par défaut), puis valide.
        OuvrirDialogPoseRecurrenteDepuisSamedi(this, grille);
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']")),
            TimeSpan.FromSeconds(10));

        // Then — le slot enregistré n'est PAS conditionné (comportement s29 strictement inchangé par défaut).
        using var scope = api.Services.CreateScope();
        var enregistre = Assert.Single(scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.False(enregistre.ConditionneGarde);
    }
}
