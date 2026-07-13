using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 27 — S3 — Rejets d'écriture d'un lieu — libellé vide ou en doublon (@back)
//   Miroir strict des rejets du référentiel acteurs (R5/R6) et rôles (R10) : un libellé vide (ou
//   tout-espaces) et un libellé déjà présent sont refusés AVANT toute écriture — aucun lieu vide ni
//   doublon persisté, référentiel inchangé, motif clair.
public class Scenario3_RejetsEcritureLieu
{
    private const string Ecole = "école"; // lieu déjà présent au référentiel

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné « école » présent, quand un parent ajoute un libellé vide → rejet sans écriture ;
    // quand il ajoute « école » (doublon) → rejet sans écriture. Le référentiel reste inchangé.
    [Fact]
    public void Acceptation_Should_Rejeter_sans_ecriture_le_libelle_vide_et_le_doublon_When_le_parent_tente_ces_ajouts()
    {
        var referentiel = new FakeReferentielActivites().AvecActivite(Ecole);
        var handler = new AjouterActiviteHandler(referentiel, referentiel);
        var avant = referentiel.EnumererActivites().Count;

        var vide = handler.Handle(new AjouterActiviteCommand("   "));
        Assert.False(vide.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(vide.Motif)); // motif clair

        var doublon = handler.Handle(new AjouterActiviteCommand(Ecole));
        Assert.False(doublon.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(doublon.Motif)); // motif clair

        // Aucune écriture : le référentiel est resté strictement inchangé (ni lieu vide, ni doublon)
        Assert.Equal(avant, referentiel.EnumererActivites().Count);
    }

    // ---------- Test #1 — Driver : libellé vide rejeté sans écriture ----------
    // Contradiction : sans garde, un libellé vide serait persisté. Force le refus AVANT toute écriture.
    [Fact]
    public void Should_Rejeter_sans_ecriture_When_le_libelle_est_vide()
    {
        var referentiel = new FakeReferentielActivites();
        var handler = new AjouterActiviteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new AjouterActiviteCommand("   "));

        Assert.False(resultat.EstSucces);
        Assert.Empty(referentiel.EnumererActivites());
    }

    // ---------- Test #2 — Driver : libellé en doublon rejeté sans écriture ----------
    // Contradiction : l'impl minimale du #1 persiste tout libellé non vide, dont un doublon. Force la
    // garde d'unicité du libellé, lue sur le référentiel courant.
    [Fact]
    public void Should_Rejeter_sans_ecriture_le_second_ajout_When_le_libelle_est_deja_present()
    {
        var referentiel = new FakeReferentielActivites().AvecActivite(Ecole);
        var handler = new AjouterActiviteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new AjouterActiviteCommand(Ecole));

        Assert.False(resultat.EstSucces);
        Assert.Single(referentiel.EnumererActivites()); // toujours un seul « école »
    }
}
