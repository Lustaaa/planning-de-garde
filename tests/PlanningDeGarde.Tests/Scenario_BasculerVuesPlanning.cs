using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 2 — Basculer entre les vues prédéfinies (@nominal, backend read model vue/span)
//   Given un cycle de fond N=2, index 0 → Alice (vert), index 1 → Bruno (bleu)
//   And aujourd'hui = mercredi 10/06/2026, semaine en cours lundi 08/06/2026
//   When je sélectionne la vue <vue>
//   Then la fenêtre affiche <nb_lignes> ligne(s), du <premier_jour> au <dernier_jour> inclus
//   And chaque case reste résolue « surcharge > fond > neutre » à sa propre date
//
// Projection backend GrilleAgendaQuery.Projeter(ancre, vue) — testée sans Blazor, ancre injectée.
public class Scenario_BasculerVuesPlanning
{
    private static readonly DateOnly Lundi_08_06_2026 = new(2026, 6, 8);
    private static readonly DateOnly Mercredi_10_06_2026 = new(2026, 6, 10);

    private const string Alice = "alice";
    private const string Bruno = "bruno";

    // Cycle N=2 : index 0 → Alice (ISO paire), index 1 → Bruno (ISO impaire).
    private static GrilleAgendaQuery QueryAvecCycle(IPeriodeRepository? periodes = null)
        => new(new FakeSlotRepository(), periodes ?? new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [Alice] = "#0a0", [Bruno] = "#00b" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Alice] = "Alice", [Bruno] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = Alice, [1] = Bruno })),
            new FakeEnumerationActeursFoyer(new[] { Alice, Bruno }));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Redimensionner_la_fenetre_selon_la_vue_choisie_Semaine_7j_QuatreSemaines_28j_Mois_semaines_ISO_du_mois_When_la_grille_est_projetee_a_l_ancre_du_08_06_2026()
    {
        var query = QueryAvecCycle();

        // Semaine → 7 jours / 1 ligne (08 → 14/06)
        var semaine = query.Projeter(Lundi_08_06_2026, VuePlanning.Semaine);
        Assert.Single(semaine.Semaines);
        Assert.Equal(7, semaine.Jours.Count);
        Assert.Equal(Lundi_08_06_2026, semaine.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 14), semaine.Jours[^1].Date);

        // 4 semaines glissantes → 28 jours / 4 lignes (08/06 → 05/07)
        var quatre = query.Projeter(Lundi_08_06_2026, VuePlanning.QuatreSemaines);
        Assert.Equal(4, quatre.Semaines.Count);
        Assert.Equal(28, quatre.Jours.Count);
        Assert.Equal(Lundi_08_06_2026, quatre.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 5), quatre.Jours[^1].Date);

        // Mois → semaines ISO entières recouvrant juin 2026 → 5 lignes (01/06 → 05/07)
        var mois = query.Projeter(Mercredi_10_06_2026, VuePlanning.Mois);
        Assert.Equal(5, mois.Semaines.Count);
        Assert.Equal(new DateOnly(2026, 6, 1), mois.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 5), mois.Jours[^1].Date);

        // Résolution par date : la première semaine ISO 24 (paire) → fond Alice
        Assert.Equal("Alice", quatre.Jours[0].NomResponsable);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — vue Semaine : 7 jours / 1 ligne, du lundi 08/06 au dimanche 14/06 (TPP constant → calcul)
    [Fact]
    public void Should_Projeter_7_jours_du_lundi_08_06_au_dimanche_14_06_2026_en_une_seule_ligne_de_semaine_When_la_vue_choisie_est_Semaine_a_l_ancre_du_08_06_2026()
    {
        var query = QueryAvecCycle();

        var grille = query.Projeter(Lundi_08_06_2026, VuePlanning.Semaine);

        Assert.Equal(7, grille.Jours.Count);
        Assert.Single(grille.Semaines);
        Assert.Equal(Lundi_08_06_2026, grille.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 14), grille.Jours[^1].Date);
    }

    // Test #2 — vue 4 semaines glissantes : 28 jours / 4 lignes, du lundi 08/06 au dimanche 05/07
    // (TPP calcul → table à 3 valeurs ; un span binaire « 7 sinon 35 » échoue sur 28 j / 4 lignes)
    [Fact]
    public void Should_Projeter_28_jours_du_lundi_08_06_au_dimanche_05_07_2026_en_4_lignes_de_semaine_When_la_vue_choisie_est_4_semaines_glissantes_a_l_ancre_du_08_06_2026()
    {
        var query = QueryAvecCycle();

        var grille = query.Projeter(Lundi_08_06_2026, VuePlanning.QuatreSemaines);

        Assert.Equal(28, grille.Jours.Count);
        Assert.Equal(4, grille.Semaines.Count);
        Assert.Equal(Lundi_08_06_2026, grille.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 5), grille.Jours[^1].Date);
    }

    // Test #3 — vue Mois : semaines ISO entières recouvrant juin 2026, du lundi 01/06 au dimanche
    // 05/07 → 5 lignes (TPP table → ancrage mensuel). Le span s'ancre au MOIS calendaire de l'ancre,
    // pas au lundi de sa semaine : 01/06 (un lundi) précède l'ancre 10/06, et 30/06 tombe dans la
    // semaine finissant le 05/07.
    [Fact]
    public void Should_Projeter_les_semaines_ISO_entieres_recouvrant_juin_2026_du_lundi_01_06_au_dimanche_05_07_en_5_lignes_When_la_vue_choisie_est_Mois_a_l_ancre_du_10_06_2026()
    {
        var query = QueryAvecCycle();

        var grille = query.Projeter(Mercredi_10_06_2026, VuePlanning.Mois);

        Assert.Equal(5, grille.Semaines.Count);
        Assert.Equal(new DateOnly(2026, 6, 1), grille.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 5), grille.Jours[^1].Date);
        Assert.Equal(DayOfWeek.Monday, grille.Jours[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, grille.Jours[^1].Date.DayOfWeek);
    }

    // Test #4 — CARACTÉRISATION (early green ATTENDU, pas un driver) : changer de vue ne touche ni
    // les périodes ni le cycle ; chaque case se re-résout à SA propre date par priorité surcharge >
    // fond. Une surcharge Bruno au seul mardi 09/06 prime sur le fond Alice (ISO 24 paire), quelle que
    // soit la vue ; le lundi 08/06 voisin, sans période, reste au fond Alice. Filet de non-régression.
    [Fact]
    public void Should_Resoudre_chaque_case_par_priorite_surcharge_puis_fond_puis_neutre_a_sa_propre_date_When_la_vue_change_sans_modifier_les_periodes_ni_le_cycle()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Bruno,
            new DateTime(2026, 6, 9), new DateTime(2026, 6, 9)).Valeur!); // surcharge au seul mardi 09/06
        var query = QueryAvecCycle(periodes);

        var mardi09 = new DateOnly(2026, 6, 9);
        foreach (var vue in new[] { VuePlanning.Semaine, VuePlanning.QuatreSemaines, VuePlanning.Mois })
        {
            var grille = query.Projeter(Lundi_08_06_2026, vue);

            // surcharge : le 09/06 porte Bruno (prime sur le fond Alice) — re-résolu à sa date
            Assert.Equal("Bruno", grille.Jours.Single(j => j.Date == mardi09).NomResponsable);
            // fond : le 08/06 voisin, sans période, reste Alice (ISO 24 paire) dans toutes les vues
            Assert.Equal("Alice", grille.Jours.Single(j => j.Date == Lundi_08_06_2026).NomResponsable);
        }
    }
}
