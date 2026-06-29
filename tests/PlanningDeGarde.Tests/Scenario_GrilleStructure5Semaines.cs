using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Structure de la grille par DÉFAUT (sans vue explicite) — RE-POINTÉ par Sprint 15 Sc.3.
// Le défaut est passé de 5 semaines / 35 jours à 4 SEMAINES GLISSANTES / 28 jours (lundi de la
// semaine en cours → dimanche +27). Caractérisation structurelle du défaut (migration mécanique
// dictée par Sc.3 — PAS une régression : l'ancien 35 j / 5 lignes était l'ancien défaut).
//   Given On est le mercredi 24/06/2026 et le foyer n'a aucun slot ni période enregistrés
//   When Un Parent ouvre le hub /planning (vue par défaut)
//   Then La grille agenda affiche exactement 28 cases-jour, la première datée du lundi
//        22/06/2026 et la dernière du dimanche 19/07/2026, organisées en 4 lignes-semaines
//        de 7 jours
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Date de référence injectée
// (jamais DateTime.Now), repositories vides doublés à la main.
public class Scenario_GrilleStructure5Semaines
{
    private static readonly DateOnly Mercredi_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);
    private static readonly DateOnly Dimanche_19_07_2026 = new(2026, 7, 19);

    private static GrilleAgendaQuery QueryVide()
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Structurer_une_fenetre_de_28_jours_du_lundi_22_06_au_dimanche_19_07_2026_en_4_semaines_de_7_jours_When_un_Parent_consulte_la_grille_le_mercredi_24_06_2026_sans_aucun_slot_ni_periode()
    {
        // Given — aucun slot ni période enregistrés
        var query = QueryVide();

        // When — un Parent consulte la grille le mercredi 24/06/2026
        var grille = query.Projeter(Mercredi_24_06_2026);

        // Then — exactement 28 cases-jour, du lundi 22/06 au dimanche 19/07
        Assert.Equal(28, grille.Jours.Count);
        Assert.Equal(Lundi_22_06_2026, grille.Jours[0].Date);
        Assert.Equal(Dimanche_19_07_2026, grille.Jours[^1].Date);

        // And — organisées en 4 lignes-semaines de 7 jours consécutifs (lundi → dimanche)
        Assert.Equal(4, grille.Semaines.Count);
        Assert.All(grille.Semaines, semaine => Assert.Equal(7, semaine.Jours.Count));
        Assert.Equal(DayOfWeek.Monday, grille.Semaines[0].Jours[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, grille.Semaines[^1].Jours[^1].Date.DayOfWeek);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — cardinalité : la projection par défaut produit exactement 28 cases-jour
    [Fact]
    public void Should_Produire_exactement_28_cases_jour_When_un_Parent_consulte_la_grille_un_jour_donne_sans_donnee()
    {
        var query = QueryVide();

        var grille = query.Projeter(Mercredi_24_06_2026);

        Assert.Equal(28, grille.Jours.Count);
    }

    // Test #2 — ancrage au lundi de la semaine en cours + fenêtre datée jour à jour. La référence
    // 24/06 est un mercredi : la fenêtre démarre au lundi 22/06 (pas au 24/06) et finit 27 jours
    // plus loin, le dimanche 19/07.
    [Fact]
    public void Should_Demarrer_au_lundi_22_06_2026_et_finir_au_dimanche_19_07_2026_When_la_date_de_reference_est_le_mercredi_24_06_2026()
    {
        var query = QueryVide();

        var grille = query.Projeter(Mercredi_24_06_2026);

        Assert.Equal(Lundi_22_06_2026, grille.Jours[0].Date);
        Assert.Equal(Dimanche_19_07_2026, grille.Jours[^1].Date);
    }

    // Test #3 — partition en 4 lignes-semaines de 7 jours consécutifs lundi → dimanche.
    [Fact]
    public void Should_Regrouper_les_28_cases_en_4_semaines_de_7_jours_consecutifs_When_la_grille_est_structuree()
    {
        var query = QueryVide();

        var grille = query.Projeter(Mercredi_24_06_2026);

        Assert.Equal(4, grille.Semaines.Count);
        Assert.All(grille.Semaines, semaine =>
        {
            Assert.Equal(7, semaine.Jours.Count);
            Assert.Equal(DayOfWeek.Monday, semaine.Jours[0].Date.DayOfWeek);
            Assert.Equal(DayOfWeek.Sunday, semaine.Jours[^1].Date.DayOfWeek);
            // 7 jours consécutifs (chaque jour suit le précédent)
            for (var i = 1; i < semaine.Jours.Count; i++)
                Assert.Equal(semaine.Jours[i - 1].Date.AddDays(1), semaine.Jours[i].Date);
        });
        // les semaines couvrent la même séquence que la liste plate, dans l'ordre
        var joursDesSemaines = grille.Semaines.SelectMany(s => s.Jours).Select(j => j.Date).ToList();
        Assert.Equal(grille.Jours.Select(j => j.Date).ToList(), joursDesSemaines);
    }
}
