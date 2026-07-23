using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

/// <summary>
/// Sprint 54 — S9 (@back) — Exceptions d'occurrence « cette occurrence » (Q4) : supprimer UNE occurrence
/// d'un jour précis d'une série récurrente retire cette seule occurrence, laisse les autres occurrences du
/// même jour de semaine projetées, et laisse la série d'origine (jours + plage) INCHANGÉE (exception par
/// date persistée). Ré-exclure la même occurrence est idempotent (no-op). Frontière Application.
/// </summary>
public sealed class Scenario54_S9_ExceptionOccurrence
{
    private static readonly TimeSpan H08h30 = new(8, 30, 0);
    private static readonly TimeSpan H16h30 = new(16, 30, 0);
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Mardi_30_06 = new(2026, 6, 30);   // occurrence à supprimer
    private static readonly DateOnly Mardi_07_07 = new(2026, 7, 7);    // autre mardi, doit rester
    private static readonly DateOnly Lundi_29_06 = new(2026, 6, 29);   // même semaine, doit rester

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
    public void Should_retirer_la_seule_occurrence_du_jour_et_laisser_les_autres_et_la_serie_intacte_When_on_supprime_cette_occurrence()
    {
        // Given — « École » lun→ven pour Léa.
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", LunAuVen, H08h30, H16h30).Valeur!);
        var id = slots.AllSnapshots().Single().Id;

        // When — on supprime l'occurrence du mardi 30/06 précis (« cette occurrence »).
        var resultat = new ExclureOccurrenceRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new ExclureOccurrenceRecurrentCommand(id, Mardi_30_06));
        Assert.True(resultat.EstSucces);

        var grille = Grille(slots).Projeter(Reference_24_06_2026);

        // Then — le mardi 30/06 n'a plus l'occurrence ; le lundi 29/06 (même semaine) ET le mardi suivant 07/07 restent.
        Assert.DoesNotContain(grille.Jours.Single(j => j.Date == Mardi_30_06).Slots, s => s.Libelle == "ecole");
        Assert.Contains(grille.Jours.Single(j => j.Date == Lundi_29_06).Slots, s => s.Libelle == "ecole");
        Assert.Contains(grille.Jours.Single(j => j.Date == Mardi_07_07).Slots, s => s.Libelle == "ecole");

        // And — la série d'origine est INCHANGÉE (jours de récurrence intacts) : l'exception vit à part.
        var snapshot = slots.AllSnapshots().Single();
        Assert.Equal(LunAuVen, snapshot.JoursDeSemaine);
    }

    [Fact]
    public void Should_etre_idempotent_When_on_re_exclut_la_meme_occurrence()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", LunAuVen, H08h30, H16h30).Valeur!);
        var id = slots.AllSnapshots().Single().Id;
        var handler = new ExclureOccurrenceRecurrentHandler(slots, new FakeNotificateurPlanning());

        handler.Handle(new ExclureOccurrenceRecurrentCommand(id, Mardi_30_06));
        var resultat = handler.Handle(new ExclureOccurrenceRecurrentCommand(id, Mardi_30_06)); // ré-exclusion

        Assert.True(resultat.EstSucces);
        // Une seule exception persistée (pas de doublon) — no-op idempotent.
        Assert.Single(slots.AllSnapshots().Single().Exclusions);
    }
}
