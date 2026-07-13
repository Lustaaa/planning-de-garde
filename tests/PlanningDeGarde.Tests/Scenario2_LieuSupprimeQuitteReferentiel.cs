using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 27 — S2 — Un lieu supprimé quitte le référentiel et la validation de saisie (@back)
//   Tranche BACKEND (frontière Application) : commande/handler SupprimerLieu qui retire le lieu via le
//   port d'écriture IEditeurActivites — il cesse d'être énuméré ET n'est plus acceptable à la pose. Borne
//   s27 : un slot DÉJÀ posé sur ce lieu conserve son lieu (aucune réécriture rétroactive).
public class Scenario2_LieuSupprimeQuitteReferentiel
{
    private const string Nounou = "nounou"; // lieu historique (seed) : id = libellé

    private static (DateTime debut, DateTime fin) Creneau
        => (new DateTime(2025, 7, 15, 8, 30, 0), new DateTime(2025, 7, 15, 16, 30, 0));

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné « nounou » présent au référentiel (seed), quand un parent le supprime, alors il quitte
    // l'énumération, une NOUVELLE pose à « nounou » est refusée (aucune écriture) — mais le slot DÉJÀ
    // posé sur « nounou » conserve son lieu (borne : aucune réécriture rétroactive).
    [Fact]
    public void Acceptation_Should_Retirer_nounou_du_referentiel_et_refuser_toute_nouvelle_pose_tout_en_preservant_le_slot_deja_pose_When_le_parent_supprime_nounou()
    {
        var referentiel = new ReferentielActivitesEnMemoire();
        var slots = new FakeSlotRepository();
        var poser = new PoserSlotHandler(slots, referentiel, new FakeReferentielEnfants().AvecEnfant("lea"), new FakeNotificateurPlanning());
        var supprimer = new SupprimerActiviteHandler(referentiel);

        // Un slot est DÉJÀ posé sur « nounou » (avant suppression)
        var poseAnterieure = poser.Handle(new PoserSlotCommand("lea", Nounou, Creneau.debut, Creneau.fin));
        Assert.True(poseAnterieure.EstSucces);

        var suppression = supprimer.Handle(new SupprimerActiviteCommand(Nounou));

        Assert.True(suppression.EstSucces);
        // « nounou » ne figure plus au référentiel des lieux du foyer
        Assert.DoesNotContain(referentiel.EnumererActivites(), lieu => lieu.Id == Nounou);

        // Poser un NOUVEAU slot à « nounou » est refusé (lieu inconnu) et n'inscrit rien
        var avant = slots.AllSnapshots().Count;
        var nouvellePose = poser.Handle(new PoserSlotCommand("lea", Nounou,
            new DateTime(2025, 7, 16, 8, 30, 0), new DateTime(2025, 7, 16, 16, 30, 0)));
        Assert.False(nouvellePose.EstSucces);
        Assert.Equal(avant, slots.AllSnapshots().Count);

        // Borne : le slot DÉJÀ posé sur « nounou » conserve son lieu (aucune réécriture rétroactive)
        Assert.Contains(slots.AllSnapshots(), s => s.LieuId == Nounou);
    }

    // ---------- Test #1 — Driver : une suppression fait DISPARAÎTRE le lieu de l'énumération ----------
    // Contradiction : le handler ne retire rien (stub) — le lieu reste énuméré. Force le retrait via le
    // port d'écriture IEditeurActivites, de sorte que le lieu cesse d'être énuméré.
    [Fact]
    public void Should_Faire_disparaitre_le_lieu_de_l_enumeration_When_le_parent_supprime_un_lieu()
    {
        var referentiel = new FakeReferentielActivites().AvecActivite(Nounou);
        var handler = new SupprimerActiviteHandler(referentiel);

        var resultat = handler.Handle(new SupprimerActiviteCommand(Nounou));

        Assert.True(resultat.EstSucces);
        Assert.DoesNotContain(referentiel.EnumererActivites(), lieu => lieu.Id == Nounou);
    }
}
