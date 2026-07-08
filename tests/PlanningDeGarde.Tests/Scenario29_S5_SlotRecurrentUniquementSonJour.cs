using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S5 — Le slot récurrent n'apparaît sur aucun autre jour de semaine (@back)
//   Étant donné un slot récurrent enregistré le samedi de 11h30 à 12h15 au lieu "Piscine"
//   Quand on projette la grille agenda sur une fenêtre de 4 semaines
//   Alors aucune case d'un jour autre que samedi ne porte l'entrée "Piscine" 11h30–12h15
//
// Limite du filtre jour-de-semaine (déjà forcé par S4 : « exactement 4 occurrences dans la
// fenêtre »). Garde de non-fuite : aucune occurrence hors du jour de récurrence.
public class Scenario29_S5_SlotRecurrentUniquementSonJour
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);

    [Fact]
    public void Should_ne_porter_le_slot_Piscine_sur_aucune_case_d_un_jour_autre_que_samedi_When_le_recurrent_est_sur_le_samedi()
    {
        var recurrents = new FakeSlotRecurrentRepository();
        recurrents.Enregistrer(SlotRecurrent
            .Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);

        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

        var grille = query.Projeter(Reference_24_06_2026);

        // Aucune case d'un jour ≠ samedi ne porte l'entrée "Piscine".
        Assert.All(
            grille.Jours.Where(j => j.Date.DayOfWeek != DayOfWeek.Saturday),
            j => Assert.DoesNotContain(j.Slots, s => s.Libelle == "piscine"));
    }
}
