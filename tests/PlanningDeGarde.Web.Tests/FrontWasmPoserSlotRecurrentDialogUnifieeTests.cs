using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sprint 29 — S8 (🖥️ scénario IHM, rework G3) : poser un slot RÉCURRENT
/// depuis la dialog « Poser un slot » UNIFIÉE (retour PO : pas de dialog/bouton séparés). Sur la grille
/// réellement câblée (front WASM <see cref="Web.Components.Planning.PlanningPartage"/> + API distante réelle,
/// store réel, projection réelle, canal d'écriture HTTP réel), cocher « Répéter chaque semaine » dans la
/// dialog de pose ordinaire bascule vers le chemin récurrent (jour de semaine déduit de la case cliquée) ;
/// la pose validée matérialise ses occurrences sur CHAQUE case du bon jour de la fenêtre.
///
/// Anti « vert qui ment » : si l'option de récurrence n'existe pas dans la dialog unique, si le chemin
/// récurrent n'est pas emprunté, ou si l'occurrence ne se matérialise pas, l'observable reste vide → rouge.
/// Un bUnit à doublure de transport ne verrait ni ce câblage distant ni la projection réelle.
/// </summary>
public sealed class FrontWasmPoserSlotRecurrentDialogUnifieeTests : TestContext
{
    // Lundi 29/06/2026 : ancre de référence → fenêtre 4 semaines 29/06→26/07, qui couvre 4 samedis.
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly string[] SamedisVisibles = { "04/07", "11/07", "18/07", "25/07" };

    private static void OuvrirDialogPoseDepuisSamedi(Bunit.TestContext ctx, IRenderedComponent<Web.Components.Planning.PlanningPartage> grille)
    {
        grille.WaitForAssertion(
            () =>
            {
                ctx.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "04/07").Click());
                ctx.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_Materialiser_le_slot_Piscine_11h30_12h15_sur_chaque_samedi_visible_When_un_Parent_coche_Repeter_chaque_semaine_dans_la_dialog_de_pose()
    {
        // Given — la grille réellement câblée à l'API distante ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // … aucune occurrence « piscine » n'est encore présente sur les samedis visibles.
        foreach (var samedi in SamedisVisibles)
            Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, samedi).QuerySelectorAll("[data-testid='slot-case']"));

        // When — un Parent ouvre la dialog « Poser un slot » depuis le samedi 04/07 (aucune dialog séparée),
        // coche « Répéter chaque semaine », choisit « piscine », 11:30 → 12:15, et valide.
        OuvrirDialogPoseDepuisSamedi(this, grille);
        this.SurDispatcher(() => grille.Find("[data-testid='champ-repeter-hebdo']").Change(true));
        grille.WaitForElement("[data-testid='champ-heure-debut']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("piscine"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut']").Change("11:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin']").Change("12:15"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — la dialog se ferme ET le slot « piscine » 11:30–12:15 apparaît sur CHAQUE samedi visible.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                foreach (var samedi in SamedisVisibles)
                {
                    var slot = GrilleRuntimeHarness.CaseDuJour(grille, samedi)
                        .QuerySelectorAll("[data-testid='slot-case']")
                        .Single(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Contains("piscine"));
                    Assert.Contains("11:30", slot.QuerySelector(".grille-slot-horaire")!.TextContent);
                    Assert.Contains("12:15", slot.QuerySelector(".grille-slot-horaire")!.TextContent);
                }
            },
            TimeSpan.FromSeconds(10));

        // … et le slot récurrent a réellement transité jusqu'au store distant (rempart anti vert-qui-ment).
        using var scope = api.Services.CreateScope();
        var enregistre = Assert.Single(scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.Equal("piscine", enregistre.LieuId);
        Assert.Equal(DayOfWeek.Saturday, enregistre.JourDeSemaine);
        Assert.Equal(new TimeSpan(11, 30, 0), enregistre.HeureDebut);
        Assert.Equal(new TimeSpan(12, 15, 0), enregistre.HeureFin);
    }

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_message_sans_rien_enregistrer_When_la_plage_du_slot_recurrent_est_invalide()
    {
        // Given — la grille réellement câblée ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // When — un Parent coche « Répéter chaque semaine » et valide une plage NON positive (fin 11:30 ≤ début 12:15).
        OuvrirDialogPoseDepuisSamedi(this, grille);
        this.SurDispatcher(() => grille.Find("[data-testid='champ-repeter-hebdo']").Change(true));
        grille.WaitForElement("[data-testid='champ-heure-debut']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("piscine"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut']").Change("12:15"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin']").Change("11:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — dialog ouverte + message dans la dialog + aucune occurrence sur les samedis.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                var message = grille.Find("[data-testid='dialog-poser-slot'] [data-testid='motif-echec']");
                Assert.False(string.IsNullOrWhiteSpace(message.TextContent));
                Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "04/07").QuerySelectorAll("[data-testid='slot-case']"));
            },
            TimeSpan.FromSeconds(10));

        // … et aucun slot récurrent n'a été enregistré au store distant.
        using var scope = api.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
    }
}
