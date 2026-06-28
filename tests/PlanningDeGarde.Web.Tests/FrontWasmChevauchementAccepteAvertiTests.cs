using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ scénario IHM, <c>@limite</c>) — poser un slot qui en
/// chevauche un autre est <b>accepté</b> (règle 16, acquise s01) et <b>signalé sans bloquer</b> : la
/// dialog se ferme (issue succès, comme tout succès), le slot chevauchant réapparaît, le slot existant
/// reste présent, et un <b>avertissement de chevauchement s'affiche à part</b> (bandeau/toast) <b>non
/// bloquant</b>. Seul l'<b>habillage IHM non bloquant</b> est neuf (test #2) ; la fermeture (test #1)
/// est une caractérisation couverte par l'issue succès du Sc.1.
///
/// Anti « vert qui ment » : on rend la vraie grille câblée à l'API distante réelle (store réel,
/// projection réelle). Le slot chevauchant doit être réellement enregistré et l'avertissement provenir
/// du chemin réel (read model de chevauchement), pas d'une logique métier dupliquée dans l'UI.
/// </summary>
public sealed class FrontWasmChevauchementAccepteAvertiTests : TestContext
{
    // Lundi 22/06/2026 : la case contenant déjà un slot « école » 08:00 → 12:00 (semé pour Léa).
    private static readonly DateTime Lundi_22_06_2026 = new(2026, 6, 22);

    [Fact]
    public void Should_Fermer_la_dialog_faire_reapparaitre_le_slot_chevauchant_et_signaler_l_avertissement_a_part_sans_bloquer_When_un_parent_pose_un_slot_qui_en_chevauche_un_autre()
    {
        // Given — l'API distante réelle ; la case du lundi 22/06 contient déjà « école » 08:00 → 12:00
        // (semé dans le store réel pour l'enfant Léa, celui que la dialog vise).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<ISlotRepository>().Enregistrer(
            SlotDeLocalisation.Poser("Léa", "école",
                new DateTime(2026, 6, 22, 8, 0, 0), new DateTime(2026, 6, 22, 12, 0, 0)).Valeur!);

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_22_06_2026);

        // … l'état initial : la case du 22/06 porte le seul slot « école ».
        Assert.Single(GrilleRuntimeHarness.CaseDuJour(grille, "22/06").QuerySelectorAll("[data-testid='slot-case']"));

        // When — un Parent ouvre la dialog depuis la case du 22/06, choisit « nounou » (08:30 → 16:30
        // pré-remplis, chevauchant « école » 08:00 → 12:00) et valide.
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "22/06").Click();
                grille.Find("[data-testid='action-poser-slot']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        grille.Find("[data-testid='champ-lieu']").Change("nounou");
        grille.Find("[data-testid='dialog-poser-slot'] form").Submit();

        // Then — l'écriture aboutit : la dialog se ferme (caractérisation, test #1), « nounou »
        // réapparaît, « école » reste présent (ni refusé ni dédoublonné) …
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                var libelles = GrilleRuntimeHarness.CaseDuJour(grille, "22/06")
                    .QuerySelectorAll("[data-testid='slot-case'] .grille-slot-libelle")
                    .Select(e => e.TextContent.Trim())
                    .ToList();
                Assert.Contains("nounou", libelles);
                Assert.Contains("école", libelles);
            },
            TimeSpan.FromSeconds(10));

        // … ET un avertissement de chevauchement s'affiche À PART (bandeau/toast), NON bloquant
        // (driver IHM neuf, test #2) — la grille n'est pas masquée ni la saisie rebloquée.
        grille.WaitForAssertion(
            () =>
            {
                var bandeau = grille.Find("[data-testid='avertissement-chevauchement']");
                Assert.False(string.IsNullOrWhiteSpace(bandeau.TextContent));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_Ne_pas_afficher_d_avertissement_de_chevauchement_When_la_pose_ne_chevauche_aucun_slot_existant()
    {
        // Contrôle de non-vacuité (garde-fou CP) : sans chevauchement, AUCUN bandeau ne doit apparaître —
        // sinon le bandeau serait affiché à tort à chaque pose. Store vierge : la case du 22/06 est vide.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_22_06_2026);

        Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "22/06").QuerySelectorAll("[data-testid='slot-case']"));

        // When — un Parent pose « école » sur la case du 22/06 (aucun slot existant → pas de chevauchement).
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "22/06").Click();
                grille.Find("[data-testid='action-poser-slot']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));
        grille.Find("[data-testid='champ-lieu']").Change("école");
        grille.Find("[data-testid='dialog-poser-slot'] form").Submit();

        // Then — la dialog se ferme, le slot réapparaît, et AUCUN bandeau de chevauchement n'est affiché.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                Assert.Single(GrilleRuntimeHarness.CaseDuJour(grille, "22/06").QuerySelectorAll("[data-testid='slot-case']"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(grille.FindAll("[data-testid='avertissement-chevauchement']"));
    }
}
