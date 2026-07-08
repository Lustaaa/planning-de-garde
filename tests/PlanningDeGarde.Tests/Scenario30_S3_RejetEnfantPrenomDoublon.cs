using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S3 — Rejet d'un enfant au prénom doublon (@back)
//   Étant donné un foyer dont le référentiel d'enfants contient déjà "Léa"
//   Quand un Parent ajoute un enfant de prénom "Léa"
//   Alors la commande échoue avec un motif clair (prénom déjà existant)
//   Et aucun second enfant "Léa" n'est enregistré
//   Et le référentiel d'enfants est inchangé
//
// Miroir strict du rejet « libellé déjà défini » du référentiel de lieux (s27 S3, R6) : unicité du
// prénom lue sur le référentiel courant, refus AVANT toute écriture.
public class Scenario30_S3_RejetEnfantPrenomDoublon
{
    private const string Lea = "Léa";

    [Fact]
    public void Acceptation_Should_Rejeter_sans_ecriture_ni_diffusion_le_second_Lea_When_le_referentiel_contient_deja_Lea()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant("enfant-lea", Lea);
        var notificateur = new FakeNotificateurPlanning();
        var handler = new AjouterEnfantHandler(referentiel, referentiel, notificateur);
        var avant = referentiel.EnumererEnfants().Count;

        var resultat = handler.Handle(new AjouterEnfantCommand(Lea));

        // La commande échoue avec un motif clair
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // Aucun second "Léa" n'est enregistré : le référentiel est strictement inchangé
        Assert.Equal(avant, referentiel.EnumererEnfants().Count);
        Assert.Single(referentiel.EnumererEnfants(), e => e.Prenom == Lea);

        // Aucune diffusion
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }
}
