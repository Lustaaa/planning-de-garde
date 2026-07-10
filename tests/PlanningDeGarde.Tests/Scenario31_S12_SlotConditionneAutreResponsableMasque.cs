using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.12 (D1) — Limite : jour où l'enfant n'est pas chez le poseur → occurrence masquée (@back)
//   Étant donné le même slot conditionné du scénario 11 (posé par "poseur")
//   Et un jour de récurrence OÙ la résolution désigne un AUTRE responsable que "poseur"
//   Quand la grille projette les occurrences
//   Alors aucune occurrence du slot n'est projetée ce jour-là (occurrence masquée)
//
// Frontière Application (GrilleAgendaQuery). Discriminant fort : le jour masqué N'est PAS neutre — il porte
// un responsable RÉSOLU mais DIFFÉRENT (autre parent en surcharge). Le masquage vient donc bien de la
// comparaison responsable == poseur, pas d'une simple absence de résolution.
public class Scenario31_S12_SlotConditionneAutreResponsableMasque
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly LundiPoseur_29_06 = new(2026, 6, 29);       // "poseur" responsable
    private static readonly DateOnly LundiAutre_06_07 = new(2026, 7, 6);         // "autre" responsable

    private static FakeSlotRecurrentRepository SlotConditionnePoseurLeLundi()
    {
        var repo = new FakeSlotRecurrentRepository();
        repo.Enregistrer(SlotRecurrent.Poser(
            "lea", "ecole", DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0),
            conditionneGarde: true, poseurId: "poseur").Valeur!);
        return repo;
    }

    // Deux lundis à responsable RÉSOLU : "poseur" le 29/06, "autre" le 06/07 (surcharges distinctes).
    private static FakePeriodeRepository PoseurPuisAutre()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "poseur", LundiPoseur_29_06.ToDateTime(TimeOnly.MinValue), LundiPoseur_29_06.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "autre", LundiAutre_06_07.ToDateTime(TimeOnly.MinValue), LundiAutre_06_07.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query()
        => new(
            new FakeSlotRepository(),
            PoseurPuisAutre(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["poseur"] = "bleu", ["autre"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["poseur"] = "Poseur", ["autre"] = "Autre" }),
            slotsRecurrents: SlotConditionnePoseurLeLundi());

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_masquer_l_occurrence_le_jour_ou_un_autre_responsable_est_resolu_When_le_slot_est_conditionne()
    {
        var grille = Query().Projeter(Reference_24_06_2026);

        // Then — le lundi où "Autre" est résolu responsable : occurrence du slot conditionné MASQUÉE…
        var caseAutre = grille.Jours.Single(j => j.Date == LundiAutre_06_07);
        Assert.DoesNotContain(caseAutre.Slots, s => s.Libelle == "ecole");

        // … alors même que ce jour porte bien un responsable RÉSOLU mais DIFFÉRENT (Autre), pas le neutre :
        // le masquage vient de la comparaison responsable == poseur, pas d'une absence de résolution.
        Assert.Equal("Autre", caseAutre.NomResponsable);

        // … et le contrôle : le lundi où "Poseur" est responsable porte bien l'occurrence (non vacant).
        var casePoseur = grille.Jours.Single(j => j.Date == LundiPoseur_29_06);
        Assert.Contains(casePoseur.Slots, s => s.Libelle == "ecole");
    }
}
