using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S2 — Rejet d'un slot récurrent sur un lieu inconnu du foyer (@back)
//   Étant donné un foyer dont le référentiel de lieux NE contient PAS "Dojo"
//   Quand un Parent pose un slot récurrent le mercredi au lieu "Dojo"
//   Alors la commande échoue avec un motif clair (lieu inexistant)
//   Et aucun slot récurrent n'est enregistré
//   Et aucune diffusion n'est déclenchée
//
// Miroir strict de PoserSlot : validation d'existence du lieu au handler (référentiel vivant).
public class Scenario29_S2_PoserSlotRecurrentLieuInconnu
{
    [Fact]
    public void Should_rejeter_sans_ecriture_ni_diffusion_When_le_lieu_du_slot_recurrent_est_inconnu_du_foyer()
    {
        // Given — le foyer NE connaît PAS le lieu "Dojo".
        var slots = new FakeSlotRecurrentRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("piscine"); // "dojo" absent
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotRecurrentHandler(slots, lieux, notificateur);

        // When — un Parent pose un slot récurrent le mercredi au lieu "Dojo".
        var resultat = handler.Handle(
            new SlotRecurrentBuilder().DansLieu("dojo").LeJour(DayOfWeek.Wednesday).Build());

        // Then — échec avec un motif clair (lieu inexistant).
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // And — aucun slot récurrent n'est enregistré.
        Assert.Empty(slots.AllSnapshots());

        // And — aucune diffusion n'est déclenchée.
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }
}
