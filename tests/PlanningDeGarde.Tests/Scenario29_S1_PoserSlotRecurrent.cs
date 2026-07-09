using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S1 — Poser un slot récurrent hebdomadaire valide (@back)
//   Étant donné un foyer dont le référentiel de lieux contient "Piscine" et un enfant déclaré
//   Quand un Parent pose un slot récurrent le samedi de 11h30 à 12h15 au lieu "Piscine"
//   Alors la commande réussit
//   Et le slot récurrent est enregistré avec un identifiant stable neuf (jamais un libellé)
//   Et son snapshot porte : jour = samedi, début = 11h30, fin = 12h15, lieu, enfant
//   Et la diffusion temps réel de mise à jour est déclenchée
//
// Boucle externe à la frontière Application (handler + ports, doublures à la main).
public class Scenario29_S1_PoserSlotRecurrent
{
    private static PoserSlotRecurrentHandler HandlerAvecLieuPiscine(
        out FakeSlotRecurrentRepository slots, out FakeNotificateurPlanning notificateur)
    {
        slots = new FakeSlotRecurrentRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("piscine");
        notificateur = new FakeNotificateurPlanning();
        return new PoserSlotRecurrentHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), notificateur);
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_enregistrer_le_slot_recurrent_avec_id_stable_snapshot_complet_et_notifier_When_un_Parent_pose_un_slot_recurrent_valide()
    {
        // Given — le foyer connaît le lieu "Piscine".
        var handler = HandlerAvecLieuPiscine(out var slots, out var notificateur);
        var commande = new SlotRecurrentBuilder()
            .PourEnfant("lea").DansLieu("piscine")
            .LeJour(DayOfWeek.Saturday).De(new TimeSpan(11, 30, 0)).A(new TimeSpan(12, 15, 0))
            .Build();

        // When — un Parent pose le slot récurrent.
        var resultat = handler.Handle(commande);

        // Then — la commande réussit.
        Assert.True(resultat.EstSucces);

        // And — le slot récurrent est enregistré avec un identifiant stable neuf (jamais un libellé).
        var enregistre = Assert.Single(slots.AllSnapshots());
        Assert.False(string.IsNullOrEmpty(enregistre.Id), "le slot récurrent enregistré doit porter un identifiant stable.");
        Assert.NotEqual("piscine", enregistre.Id);

        // And — son snapshot porte jour, plage, lieu, enfant.
        Assert.Equal("lea", enregistre.EnfantId);
        Assert.Equal("piscine", enregistre.LieuId);
        Assert.Equal(DayOfWeek.Saturday, enregistre.JourDeSemaine);
        Assert.Equal(new TimeSpan(11, 30, 0), enregistre.HeureDebut);
        Assert.Equal(new TimeSpan(12, 15, 0), enregistre.HeureFin);

        // And — la diffusion temps réel de mise à jour est déclenchée.
        Assert.Equal(1, notificateur.NombreDeNotifications);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — baseline : la pose d'un slot récurrent valide est confirmée.
    [Fact]
    public void Should_confirmer_la_pose_When_le_slot_recurrent_est_valide()
    {
        var handler = HandlerAvecLieuPiscine(out _, out _);

        var resultat = handler.Handle(new SlotRecurrentBuilder().Build());

        Assert.True(resultat.EstSucces);
    }

    // Test #2 — inscription : le slot posé est inscrit dans le store récurrent partagé.
    [Fact]
    public void Should_inscrire_le_slot_recurrent_dans_le_store_When_un_Parent_a_pose_le_slot()
    {
        var handler = HandlerAvecLieuPiscine(out var slots, out _);

        handler.Handle(new SlotRecurrentBuilder().PourEnfant("lea").DansLieu("piscine").LeJour(DayOfWeek.Saturday).Build());

        var enregistre = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", enregistre.EnfantId);
        Assert.Equal("piscine", enregistre.LieuId);
        Assert.Equal(DayOfWeek.Saturday, enregistre.JourDeSemaine);
    }

    // Test #3 — notification : la pose déclenche la diffusion temps réel.
    [Fact]
    public void Should_declencher_la_diffusion_temps_reel_When_un_Parent_a_pose_le_slot_recurrent()
    {
        var handler = HandlerAvecLieuPiscine(out _, out var notificateur);

        handler.Handle(new SlotRecurrentBuilder().Build());

        Assert.Equal(1, notificateur.NombreDeNotifications);
    }
}
