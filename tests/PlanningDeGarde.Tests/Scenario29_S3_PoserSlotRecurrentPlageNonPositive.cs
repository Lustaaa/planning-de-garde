using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S3 — Rejet d'une plage horaire non positive (@back)
//   Étant donné un foyer dont le référentiel de lieux contient "Piscine"
//   Quand un Parent pose un slot récurrent le samedi de 12h15 à 11h30 au lieu "Piscine"
//   Alors la commande échoue (la durée doit être strictement positive)
//   Et aucun slot récurrent n'est enregistré
//
// L'invariant temporel (fin > début) est porté par l'agrégat SlotRecurrent (Tell-Don't-Ask),
// miroir de SlotDeLocalisation ; le handler relaie le refus sans écrire.
public class Scenario29_S3_PoserSlotRecurrentPlageNonPositive
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_rejeter_sans_ecriture_When_la_plage_horaire_du_slot_recurrent_est_non_positive()
    {
        // Given — le foyer connaît le lieu "Piscine".
        var slots = new FakeSlotRecurrentRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("piscine");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotRecurrentHandler(slots, lieux, notificateur);

        // When — un Parent pose un slot récurrent le samedi de 12h15 à 11h30 (fin ≤ début).
        var resultat = handler.Handle(new SlotRecurrentBuilder()
            .DansLieu("piscine").LeJour(DayOfWeek.Saturday)
            .De(new TimeSpan(12, 15, 0)).A(new TimeSpan(11, 30, 0)).Build());

        // Then — échec (la durée doit être strictement positive).
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // And — aucun slot récurrent n'est enregistré.
        Assert.Empty(slots.AllSnapshots());
    }

    // ---------- Test unitaire (boucle interne, TDD) — invariant dans l'agrégat ----------

    // La règle « fin > début » est un invariant de SlotRecurrent : une plage non positive n'est
    // jamais un agrégat valide (même durée nulle refusée).
    [Theory]
    [InlineData(11, 30, 11, 30)] // durée nulle
    [InlineData(12, 15, 11, 30)] // fin avant début
    public void Should_refuser_l_agregat_When_la_plage_horaire_n_est_pas_strictement_positive(
        int hDebut, int mDebut, int hFin, int mFin)
    {
        var pose = SlotRecurrent.Poser(
            "lea", "piscine", DayOfWeek.Saturday, new TimeSpan(hDebut, mDebut, 0), new TimeSpan(hFin, mFin, 0));

        Assert.False(pose.EstSucces);
    }
}
