using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.11 (D1, 2e cœur) — Nominal : slot conditionné aux jours de garde du poseur (@back)
//   Étant donné un slot récurrent posé par le parent "Poseur", conditionné "seulement les jours où
//     l'enfant est chez moi"
//   Quand la grille projette les occurrences du slot sur la fenêtre
//   Alors une occurrence est projetée uniquement les jours de récurrence OÙ la résolution désigne
//     "Poseur" responsable
//   Et le conditionnement lit la résolution (surcharge > fond), sans la modifier
//
// Frontière Application (GrilleAgendaQuery). Le slot lit désormais la responsabilité : sa matérialisation
// est bornée aux jours où ResoudreResponsable(jour) == poseur. Test DISCRIMINANT : sur les 4 lundis de la
// fenêtre, seul le 29/06 voit "Poseur" responsable (surcharge période) → l'occurrence n'y apparaît QUE.
public class Scenario31_S11_SlotConditionneJoursDeGarde
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly LundiPoseurResponsable_29_06 = new(2026, 6, 29);
    private static readonly DateOnly[] LundisSansPoseur =
    {
        new(2026, 6, 22), new(2026, 7, 6), new(2026, 7, 13),
    };

    // Slot récurrent CONDITIONNÉ posé par "poseur", le lundi 8h→9h au lieu "ecole" pour l'enfant "lea".
    private static FakeSlotRecurrentRepository SlotConditionnePoseurLeLundi()
    {
        var repo = new FakeSlotRecurrentRepository();
        repo.Enregistrer(SlotRecurrent.Poser(
            "lea", "ecole", DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0),
            conditionneGarde: true, poseurId: "poseur").Valeur!);
        return repo;
    }

    // Une seule période qui rend "poseur" responsable le lundi 29/06 (surcharge). Les autres lundis
    // n'ont ni surcharge ni fond → responsable neutre (≠ poseur).
    private static FakePeriodeRepository PoseurResponsableLe29()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "poseur", LundiPoseurResponsable_29_06.ToDateTime(TimeOnly.MinValue),
            LundiPoseurResponsable_29_06.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(IPeriodeRepository periodes, ISlotRecurrentRepository recurrents)
        => new(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { ["poseur"] = "bleu" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["poseur"] = "Poseur" }),
            slotsRecurrents: recurrents);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_projeter_l_occurrence_uniquement_les_jours_ou_le_poseur_est_responsable_When_le_slot_est_conditionne()
    {
        var grille = Query(PoseurResponsableLe29(), SlotConditionnePoseurLeLundi()).Projeter(Reference_24_06_2026);

        // Then — l'occurrence "ecole" est projetée le lundi 29/06 (poseur responsable)…
        var caseGarde = grille.Jours.Single(j => j.Date == LundiPoseurResponsable_29_06);
        Assert.Contains(caseGarde.Slots, s => s.Libelle == "ecole");

        // … et sur AUCUN autre lundi (poseur non responsable → occurrence masquée)…
        foreach (var lundi in LundisSansPoseur)
        {
            var caseSansGarde = grille.Jours.Single(j => j.Date == lundi);
            Assert.DoesNotContain(caseSansGarde.Slots, s => s.Libelle == "ecole");
        }

        // … soit exactement UNE occurrence dans la fenêtre (le seul jour de garde du poseur).
        var occurrences = grille.Jours.SelectMany(j => j.Slots).Where(s => s.Libelle == "ecole").ToList();
        Assert.Single(occurrences);
    }

    // ---------- Test unitaire (boucle interne, TDD) ----------

    // Le conditionnement LIT la résolution sans la modifier : la case du jour de garde garde son
    // responsable résolu (surcharge = poseur), le slot conditionné ne change pas la responsabilité.
    [Fact]
    public void Should_laisser_la_resolution_de_responsabilite_inchangee_When_le_slot_conditionne_est_projete()
    {
        var caseGarde = Query(PoseurResponsableLe29(), SlotConditionnePoseurLeLundi())
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == LundiPoseurResponsable_29_06);

        Assert.Equal("Poseur", caseGarde.NomResponsable);
        Assert.Equal("bleu", caseGarde.CouleurResponsable);
    }
}
