using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 3 — Fenêtre par défaut à l'ouverture = 4 semaines glissantes (@limite, backend)
//   Given aujourd'hui = mercredi 10/06/2026, semaine en cours lundi 08/06/2026
//   When le hub /planning est ouvert SANS navigation (vue par défaut)
//   Then la grille montre 4 semaines glissantes (28 j / 4 lignes) du lundi 08/06 au dimanche
//        05/07/2026, dernière ligne au lundi 29/06, fond de la 1ʳᵉ semaine = Alice
//
// Bascule du DÉFAUT 5 → 4 semaines : le défaut de Projeter(dateReference) (sans vue explicite)
// passe de 35 j / 5 lignes à 28 j / 4 lignes. Un seul DRIVER (#1, le défaut) ; #2 et #3
// caractérisent (early green attendu) le span déjà acquis par Sc.2 et la résolution du fond par date.
public class Scenario_DefautQuatreSemaines
{
    private static readonly DateOnly Mercredi_10_06_2026 = new(2026, 6, 10);
    private static readonly DateOnly Lundi_08_06_2026 = new(2026, 6, 8);
    private static readonly DateOnly Dimanche_05_07_2026 = new(2026, 7, 5);
    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);

    private const string Alice = "alice";
    private const string Bruno = "bruno";

    private static GrilleAgendaQuery QueryVide()
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    // Cycle N=2 : index 0 → Alice (ISO paire), index 1 → Bruno (ISO impaire). Le lundi 08/06/2026
    // est en ISO 24 (paire) → fond Alice.
    private static GrilleAgendaQuery QueryAvecCycle()
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [Alice] = "#0a0", [Bruno] = "#00b" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Alice] = "Alice", [Bruno] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = Alice, [1] = Bruno })),
            new FakeEnumerationActeursFoyer(new[] { Alice, Bruno }));

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — DRIVER : sans vue explicite, la projection produit 28 jours / 4 lignes depuis le
    // lundi de la semaine en cours (08/06 → 05/07). L'ancien défaut (35 j / 5 lignes) échoue.
    [Fact]
    public void Should_Projeter_28_jours_en_4_lignes_depuis_le_lundi_de_la_semaine_en_cours_When_le_planning_est_projete_sans_vue_explicite()
    {
        var grille = QueryVide().Projeter(Mercredi_10_06_2026);

        Assert.Equal(28, grille.Jours.Count);
        Assert.Equal(4, grille.Semaines.Count);
        Assert.Equal(Lundi_08_06_2026, grille.Jours[0].Date);
        Assert.Equal(Dimanche_05_07_2026, grille.Jours[^1].Date);
    }

    // Test #2 — CARACTÉRISATION (early green ATTENDU, filet @limite — pas un driver) : la dernière
    // ligne (4ᵉ semaine) commence au lundi 29/06/2026. Couvert par #1 : le span de 28 j place
    // mécaniquement la 4ᵉ ligne au 29/06 (08/06 + 21 j). Filet de non-régression du défaut.
    [Fact]
    public void Should_Demarrer_la_derniere_ligne_au_lundi_29_06_2026_When_la_fenetre_par_defaut_de_4_semaines_est_projetee_le_10_06_2026()
    {
        var grille = QueryVide().Projeter(Mercredi_10_06_2026);

        Assert.Equal(Lundi_29_06_2026, grille.Semaines[^1].Jours[0].Date);
    }

    // Test #3 — CARACTÉRISATION (early green ATTENDU, filet @limite — pas un driver) : le fond de la
    // première semaine affichée (lundi 08/06, ISO 24 paire → index 0) est Alice. Couvert par la
    // résolution ResponsableDeFond(date) par date déjà acquise (Sc.2). Filet de non-régression.
    [Fact]
    public void Should_Afficher_Alice_en_fond_de_la_premiere_semaine_affichee_When_la_fenetre_par_defaut_demarre_au_lundi_08_06_2026()
    {
        var grille = QueryAvecCycle().Projeter(Mercredi_10_06_2026);

        Assert.Equal("Alice", grille.Jours[0].NomResponsable);
        Assert.Equal(Lundi_08_06_2026, grille.Jours[0].Date);
    }
}
