using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

/// <summary>
/// Sprint 54 — S7 (@back) — Exclusion vacances scolaires : une plage d'exclusion [J1..J2] rattachée à une
/// activité récurrente fait que la projection ne matérialise AUCUNE occurrence sur cet intervalle (bornes
/// incluses), tout en projetant normalement hors de la plage. Acceptation à la frontière Application
/// (handler d'exclusion + projection de lecture, doublures à la main).
/// </summary>
public sealed class Scenario54_S7_ExclusionVacances
{
    private static readonly TimeSpan H08h30 = new(8, 30, 0);
    private static readonly TimeSpan H16h30 = new(16, 30, 0);
    // Référence mercredi 24/06/2026 → lundi de la semaine 22/06 ; fenêtre ~4 semaines.
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    // Plage d'exclusion = semaine du lundi 29/06 au dimanche 05/07/2026 (2ᵉ semaine de la fenêtre).
    private static readonly DateOnly Vacances_Debut = new(2026, 6, 29);
    private static readonly DateOnly Vacances_Fin = new(2026, 7, 5);

    private static readonly DayOfWeek[] LunAuVen =
        { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    private static GrilleAgendaQuery Grille(ISlotRecurrentRepository recurrents)
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

    [Fact]
    public void Should_ne_projeter_aucune_occurrence_pendant_les_vacances_mais_projeter_normalement_hors_plage_When_une_plage_d_exclusion_est_rattachee()
    {
        // Given — « École » lun→ven pour Léa, dans le store.
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", LunAuVen, H08h30, H16h30).Valeur!);
        var id = slots.AllSnapshots().Single().Id;

        // When — on rattache une plage d'exclusion [29/06..05/07] (vacances) à cette activité.
        var resultat = new AjouterExclusionRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new AjouterExclusionRecurrentCommand(id, Vacances_Debut, Vacances_Fin));
        Assert.True(resultat.EstSucces);

        var grille = Grille(slots).Projeter(Reference_24_06_2026);

        // Then — AUCUNE occurrence « ecole » sur les jours ouvrés de la semaine exclue [29/06..03/07].
        foreach (var jour in grille.Jours.Where(j => j.Date >= Vacances_Debut && j.Date <= Vacances_Fin))
            Assert.DoesNotContain(jour.Slots, s => s.Libelle == "ecole");

        // And — hors plage, les occurrences sont projetées normalement (semaine précédente ET suivante).
        var lunPrecedent = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 22));
        var lunSuivant = grille.Jours.Single(j => j.Date == new DateOnly(2026, 7, 6));
        Assert.Contains(lunPrecedent.Slots, s => s.Libelle == "ecole");
        Assert.Contains(lunSuivant.Slots, s => s.Libelle == "ecole");
    }

    [Fact]
    public void Should_persister_la_plage_d_exclusion_dans_le_snapshot_When_elle_est_rattachee()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", LunAuVen, H08h30, H16h30).Valeur!);
        var id = slots.AllSnapshots().Single().Id;

        new AjouterExclusionRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new AjouterExclusionRecurrentCommand(id, Vacances_Debut, Vacances_Fin));

        var snapshot = slots.AllSnapshots().Single();
        Assert.Contains(new PlageExclusion(Vacances_Debut, Vacances_Fin), snapshot.Exclusions);
    }

    [Fact]
    public void Should_reprendre_les_occurrences_When_la_plage_d_exclusion_est_retiree()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", LunAuVen, H08h30, H16h30).Valeur!);
        var id = slots.AllSnapshots().Single().Id;
        new AjouterExclusionRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new AjouterExclusionRecurrentCommand(id, Vacances_Debut, Vacances_Fin));

        // When — on retire la plage d'exclusion.
        new SupprimerExclusionRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new SupprimerExclusionRecurrentCommand(id, Vacances_Debut, Vacances_Fin));

        // Then — les occurrences reprennent sur l'intervalle réintégré.
        var grille = Grille(slots).Projeter(Reference_24_06_2026);
        var lundiReintegre = grille.Jours.Single(j => j.Date == Vacances_Debut);
        Assert.Contains(lundiReintegre.Slots, s => s.Libelle == "ecole");
    }
}
