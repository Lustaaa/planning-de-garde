using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S7 — Rejet d'une pose de slot référençant un enfant inconnu du foyer (@back)
//   Étant donné un foyer dont le référentiel d'enfants NE contient PAS l'identifiant "enfant-x"
//   Quand un Parent pose un slot en référençant l'enfant "enfant-x"
//   Alors la commande échoue avec un motif clair (enfant inexistant)
//   Et aucun slot n'est enregistré
//   Et aucune diffusion n'est déclenchée
//
// Miroir strict de la validation « lieu inconnu » (s27/s29) : existence de l'enfant lue sur le
// référentiel VIVANT (IEnumerationEnfants) au handler, pour le slot PONCTUEL comme RÉCURRENT — l'enfant
// n'est plus un fantôme transmis à l'aveugle (Session.EnfantId), il doit exister au foyer.
public class Scenario30_S7_PoserSlotEnfantInconnu
{
    // ---------- Slot PONCTUEL ----------
    [Fact]
    public void Should_Rejeter_sans_ecriture_ni_diffusion_le_slot_ponctuel_When_l_enfant_est_inconnu_du_foyer()
    {
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielActivites().AvecActivite("ecole"); // le lieu EXISTE (isole l'échec sur l'enfant)
        var enfants = new FakeReferentielEnfants().AvecEnfant("lea"); // "enfant-x" ABSENT
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, enfants, notificateur);

        var resultat = handler.Handle(new SlotBuilder().PourEnfant("enfant-x").DansLieu("ecole").Build());

        // Échec avec motif clair (enfant inexistant)
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // Aucun slot enregistré, aucune diffusion
        Assert.Empty(slots.AllSnapshots());
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }

    // ---------- Slot RÉCURRENT ----------
    [Fact]
    public void Should_Rejeter_sans_ecriture_ni_diffusion_le_slot_recurrent_When_l_enfant_est_inconnu_du_foyer()
    {
        var slots = new FakeSlotRecurrentRepository();
        var lieux = new FakeReferentielActivites().AvecActivite("piscine"); // le lieu EXISTE
        var enfants = new FakeReferentielEnfants().AvecEnfant("lea"); // "enfant-x" ABSENT
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotRecurrentHandler(slots, lieux, enfants, notificateur);

        var resultat = handler.Handle(
            new SlotRecurrentBuilder().PourEnfant("enfant-x").DansLieu("piscine").LeJour(DayOfWeek.Saturday).Build());

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(slots.AllSnapshots());
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }
}
