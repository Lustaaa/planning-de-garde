using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S4 — Le slot récurrent se matérialise sur toutes les occurrences du bon jour (@back)
//   Étant donné un slot récurrent enregistré le samedi de 11h30 à 12h15 au lieu "Piscine"
//   Quand on projette la grille agenda sur une fenêtre de 4 semaines
//   Alors chaque case de samedi de la fenêtre porte une entrée de slot "Piscine" 11h30–12h15
//   Et les bornes horaires sont identiques sur toutes les occurrences
//   Et l'entrée s'empile dans l'ordre horaire avec les slots ponctuels du même jour
//
// Projection backend GrilleAgendaQuery — testée sans Blazor, date de référence injectée.
public class Scenario29_S4_ProjectionSlotRecurrent
{
    // Mercredi 24/06/2026 : lundi de la semaine = 22/06 ; fenêtre 28 j (22/06 → 19/07) → 4 samedis.
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly TimeOnly H11h30 = new(11, 30);
    private static readonly TimeOnly H12h15 = new(12, 15);
    private static readonly DateOnly[] Samedis =
    {
        new(2026, 6, 27), new(2026, 7, 4), new(2026, 7, 11), new(2026, 7, 18),
    };

    private static FakeSlotRecurrentRepository RecurrentsAvecPiscineLeSamedi()
    {
        var repo = new FakeSlotRecurrentRepository();
        repo.Enregistrer(SlotRecurrent
            .Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(ISlotRepository slots, ISlotRecurrentRepository recurrents)
        => new(
            slots,
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_materialiser_le_slot_Piscine_11h30_12h15_sur_chaque_samedi_de_la_fenetre_When_on_projette_4_semaines()
    {
        var query = Query(new FakeSlotRepository(), RecurrentsAvecPiscineLeSamedi());

        var grille = query.Projeter(Reference_24_06_2026);

        // Then — chaque case de samedi porte l'entrée "Piscine" 11h30–12h15.
        foreach (var samedi in Samedis)
        {
            var caseSamedi = grille.Jours.Single(j => j.Date == samedi);
            var slot = Assert.Single(caseSamedi.Slots, s => s.Libelle == "piscine");
            Assert.Equal(H11h30, slot.Debut);
            Assert.Equal(H12h15, slot.Fin);
        }

        // And — exactement 4 occurrences dans la fenêtre (une par samedi), bornes identiques.
        var piscines = grille.Jours.SelectMany(j => j.Slots).Where(s => s.Libelle == "piscine").ToList();
        Assert.Equal(4, piscines.Count);
        Assert.All(piscines, s => { Assert.Equal(H11h30, s.Debut); Assert.Equal(H12h15, s.Fin); });
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence : la projection lit ISlotRecurrentRepository et matérialise l'occurrence
    // sur la case du bon jour (la grille des slots ponctuels ne lit pas les récurrents).
    [Fact]
    public void Should_faire_apparaitre_le_slot_recurrent_sur_le_premier_samedi_When_il_est_enregistre()
    {
        var query = Query(new FakeSlotRepository(), RecurrentsAvecPiscineLeSamedi());

        var grille = query.Projeter(Reference_24_06_2026);

        var premierSamedi = grille.Jours.Single(j => j.Date == Samedis[0]);
        Assert.Contains(premierSamedi.Slots, s => s.Libelle == "piscine");
    }

    // Test #2 — empilement horaire : sur un samedi portant AUSSI un slot ponctuel plus matinal,
    // le récurrent s'empile APRÈS lui (ordre horaire), fusionné dans la même liste de la case.
    [Fact]
    public void Should_empiler_le_recurrent_apres_un_slot_ponctuel_matinal_du_meme_jour_When_les_deux_couvrent_le_meme_samedi()
    {
        var ponctuels = new FakeSlotRepository();
        ponctuels.Enregistrer(SlotDeLocalisation
            .Poser("lea", "ecole", new DateTime(2026, 6, 27, 8, 0, 0), new DateTime(2026, 6, 27, 9, 0, 0)).Valeur!);

        var grille = Query(ponctuels, RecurrentsAvecPiscineLeSamedi()).Projeter(Reference_24_06_2026);

        var caseSamedi = grille.Jours.Single(j => j.Date == Samedis[0]);
        Assert.Equal(2, caseSamedi.Slots.Count);
        Assert.Equal(new[] { "ecole", "piscine" }, caseSamedi.Slots.Select(s => s.Libelle).ToArray());
    }
}
