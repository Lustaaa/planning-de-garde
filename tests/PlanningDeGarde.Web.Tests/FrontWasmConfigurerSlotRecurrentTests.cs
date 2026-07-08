using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sprint 29 — S8 (🖥️ scénario IHM) : configurer un slot RÉCURRENT depuis
/// l'IHM réellement câblée (front WASM <see cref="Web.Components.Pages.PlanningPartage"/> + API distante
/// réelle, store réel, projection réelle, canal d'écriture HTTP réel). Depuis le menu clic-case, l'entrée
/// « Poser un slot récurrent » ouvre une dialog (enfant + lieu + jour de semaine + plage horaire). La pose
/// validée matérialise ses occurrences sur CHAQUE case du bon jour de la fenêtre — relues depuis le store
/// réel via la projection distante, jamais une mutation locale.
///
/// Anti « vert qui ment » : si le menu n'expose pas l'entrée, si la dialog ne s'ouvre pas, si la pose ne
/// transite pas jusqu'au store distant, ou si l'occurrence ne se matérialise pas, l'observable reste vide
/// → rouge. Un bUnit à doublure de transport ne verrait ni ce câblage distant ni la projection réelle.
/// </summary>
public sealed class FrontWasmConfigurerSlotRecurrentTests : TestContext
{
    // Lundi 29/06/2026 : ancre de référence → fenêtre 4 semaines 29/06→26/07, qui couvre 4 samedis.
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly string[] SamedisVisibles = { "04/07", "11/07", "18/07", "25/07" };

    [Fact]
    public void Should_Materialiser_le_slot_Piscine_11h30_12h15_sur_chaque_samedi_visible_When_un_Parent_configure_un_slot_recurrent_depuis_le_menu()
    {
        // Given — la grille réellement câblée à l'API distante ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurLieux>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // … aucune occurrence « piscine » n'est encore présente sur les samedis visibles.
        foreach (var samedi in SamedisVisibles)
            Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, samedi).QuerySelectorAll("[data-testid='slot-case']"));

        // When — un Parent clique la case du samedi 04/07 → menu → « Poser un slot récurrent » → la dialog s'ouvre.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "04/07").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot-recurrent']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot-recurrent']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit le lieu « piscine » (jour pré-rempli sur le samedi de la case), 11:30 → 12:15, et valide.
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu-recurrent']").Change("piscine"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-jour-recurrent']").Change("Saturday"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut-recurrent']").Change("11:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin-recurrent']").Change("12:15"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot-recurrent'] form").Submit());

        // Then — la dialog se ferme ET le slot « piscine » 11:30–12:15 apparaît sur CHAQUE samedi visible.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot-recurrent']"));
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

        // … et le slot récurrent a réellement transité jusqu'au store distant (rempart anti vert-qui-ment) :
        // observé via le port réel de l'hôte d'API.
        using var scope = api.Services.CreateScope();
        var recurrents = scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots();
        var enregistre = Assert.Single(recurrents);
        Assert.Equal("piscine", enregistre.LieuId);
        Assert.Equal(DayOfWeek.Saturday, enregistre.JourDeSemaine);
        Assert.Equal(new TimeSpan(11, 30, 0), enregistre.HeureDebut);
        Assert.Equal(new TimeSpan(12, 15, 0), enregistre.HeureFin);
    }

    [Fact]
    public void Should_Laisser_la_dialog_ouverte_avec_message_sans_rien_enregistrer_When_la_plage_horaire_est_invalide()
    {
        // Given — la grille réellement câblée ; le lieu « piscine » existe au store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurLieux>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // When — un Parent ouvre la dialog et valide une plage NON positive (fin 11:30 ≤ début 12:15).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "04/07").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot-recurrent']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot-recurrent']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu-recurrent']").Change("piscine"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut-recurrent']").Change("12:15"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin-recurrent']").Change("11:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot-recurrent'] form").Submit());

        // Then — dialog ouverte + message dans la dialog + aucune occurrence sur les samedis.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot-recurrent']"));
                var message = grille.Find("[data-testid='dialog-poser-slot-recurrent'] [data-testid='motif-echec-recurrent']");
                Assert.False(string.IsNullOrWhiteSpace(message.TextContent));
                Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "04/07").QuerySelectorAll("[data-testid='slot-case']"));
            },
            TimeSpan.FromSeconds(10));

        // … et aucun slot récurrent n'a été enregistré au store distant.
        using var scope = api.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
    }
}
