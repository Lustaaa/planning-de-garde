using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

/// <summary>
/// Sprint 54 — S4 (@back) — Récurrence MULTI-JOURS : un récurrent porte un SET de jours (école
/// lun/mar/jeu/ven). Acceptation à la frontière Application (handler d'écriture + projection de lecture,
/// doublures à la main) : le set est persisté rattaché à l'enfant et projeté sur CHAQUE jour du set,
/// jamais les autres. Compat : un set d'un seul jour = comportement s29 inchangé. Erreur : set vide →
/// refus AVANT écriture, store intact.
/// </summary>
public sealed class Scenario54_S4_PoserSlotRecurrentMultiJours
{
    private static readonly TimeSpan H08h30 = new(8, 30, 0);
    private static readonly TimeSpan H16h30 = new(16, 30, 0);
    // Mercredi 24/06/2026 : lundi de la semaine = 22/06 ; fenêtre 28 j → 4 de chaque jour de semaine.
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);

    private static PoserSlotRecurrentHandler HandlerAvecEcoleEtLea(out FakeSlotRecurrentRepository slots)
    {
        slots = new FakeSlotRecurrentRepository();
        var lieux = new FakeReferentielActivites().AvecActivite("ecole");
        return new PoserSlotRecurrentHandler(
            slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), new FakeNotificateurPlanning());
    }

    private static GrilleAgendaQuery Grille(ISlotRecurrentRepository recurrents)
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

    [Fact]
    public void Should_persister_le_set_et_projeter_une_occurrence_chaque_jour_du_set_jamais_le_mercredi_When_on_pose_un_recurrent_multi_jours()
    {
        // Given — le foyer connaît « École » et « Léa ».
        var handler = HandlerAvecEcoleEtLea(out var slots);
        var commande = new SlotRecurrentBuilder()
            .PourEnfant("lea").DansLieu("ecole")
            .LesJours(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday)
            .De(H08h30).A(H16h30)
            .Build();

        // When — un Parent pose le récurrent multi-jours.
        var resultat = handler.Handle(commande);

        // Then — la pose réussit, le slot est persisté rattaché à « Léa » avec son set de jours.
        Assert.True(resultat.EstSucces);
        var enregistre = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", enregistre.EnfantId);
        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            enregistre.JoursDeSemaine);

        // And — la projection matérialise une occurrence sur CHAQUE lundi/mardi/jeudi/vendredi, jamais le mercredi ni le week-end.
        var grille = Grille(slots).Projeter(Reference_24_06_2026);
        var joursDuSet = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var jourCase in grille.Jours.Where(j => joursDuSet.Contains(j.Date.DayOfWeek)))
            Assert.Contains(jourCase.Slots, s => s.Libelle == "ecole");
        foreach (var horsSet in grille.Jours.Where(j => !joursDuSet.Contains(j.Date.DayOfWeek)))
            Assert.DoesNotContain(horsSet.Slots, s => s.Libelle == "ecole");
    }

    [Fact]
    public void Should_projeter_comme_s29_When_le_set_ne_porte_qu_un_seul_jour()
    {
        // Compat : un set d'un seul jour = comportement mono-jour s29 inchangé (occurrences le samedi seul).
        var handler = HandlerAvecEcoleEtLea(out var slots);

        var resultat = handler.Handle(new SlotRecurrentBuilder()
            .PourEnfant("lea").DansLieu("ecole").LesJours(DayOfWeek.Saturday).De(H08h30).A(H16h30).Build());

        Assert.True(resultat.EstSucces);
        var grille = Grille(slots).Projeter(Reference_24_06_2026);
        foreach (var samedi in grille.Jours.Where(j => j.Date.DayOfWeek == DayOfWeek.Saturday))
            Assert.Contains(samedi.Slots, s => s.Libelle == "ecole");
        foreach (var autre in grille.Jours.Where(j => j.Date.DayOfWeek != DayOfWeek.Saturday))
            Assert.DoesNotContain(autre.Slots, s => s.Libelle == "ecole");
    }

    [Fact]
    public void Should_refuser_avant_ecriture_et_laisser_le_store_intact_When_le_set_de_jours_est_vide()
    {
        var handler = HandlerAvecEcoleEtLea(out var slots);

        var resultat = handler.Handle(new SlotRecurrentBuilder()
            .PourEnfant("lea").DansLieu("ecole").LesJours().De(H08h30).A(H16h30).Build());

        Assert.False(resultat.EstSucces);
        Assert.Empty(slots.AllSnapshots());
    }

    [Fact]
    public void Should_dedoublonner_le_set_et_ne_projeter_qu_une_occurrence_par_lundi_When_le_jour_est_duplique()
    {
        var handler = HandlerAvecEcoleEtLea(out var slots);

        var resultat = handler.Handle(new SlotRecurrentBuilder()
            .PourEnfant("lea").DansLieu("ecole").LesJours(DayOfWeek.Monday, DayOfWeek.Monday).De(H08h30).A(H16h30).Build());

        Assert.True(resultat.EstSucces);
        var enregistre = Assert.Single(slots.AllSnapshots());
        Assert.Equal(new[] { DayOfWeek.Monday }, enregistre.JoursDeSemaine);

        var grille = Grille(slots).Projeter(Reference_24_06_2026);
        var premierLundi = grille.Jours.First(j => j.Date.DayOfWeek == DayOfWeek.Monday);
        Assert.Single(premierLundi.Slots, s => s.Libelle == "ecole");
    }
}
