using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S2 — Rejet d'un enfant au prénom vide (@back)
//   Étant donné un foyer configuré
//   Quand un Parent ajoute un enfant de prénom "" (vide ou blancs)
//   Alors la commande échoue avec un motif clair (prénom requis)
//   Et aucun enfant n'est enregistré
//   Et aucune diffusion n'est déclenchée
//
// Miroir strict du rejet « libellé vide » du référentiel de lieux (s27 S3, R5) : un prénom vide (ou
// tout-espaces) est refusé AVANT toute génération d'id, toute écriture et toute diffusion.
public class Scenario30_S2_RejetEnfantPrenomVide
{
    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Rejeter_sans_ecriture_ni_diffusion_When_le_parent_ajoute_un_enfant_au_prenom_vide()
    {
        var referentiel = new FakeReferentielEnfants();
        var notificateur = new FakeNotificateurPlanning();
        var handler = new AjouterEnfantHandler(referentiel, referentiel, notificateur);

        var resultat = handler.Handle(new AjouterEnfantCommand("   "));

        // La commande échoue avec un motif clair
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // Aucun enfant n'est enregistré
        Assert.Empty(referentiel.EnumererEnfants());

        // Aucune diffusion n'est déclenchée
        Assert.Equal(0, notificateur.NombreDeNotifications);
    }
}
