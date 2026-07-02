using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 24 — Sc.1 — Activation d'un compte Inactif (@back)
//   Tranche BACKEND (frontière Application) : commande/handler ActiverCompte qui cible un compte
//   existant par son id stable opaque (s22) et fait passer son statut Inactif→Actif via le port
//   d'écriture IEditeurComptes (aucun nouvel agrégat, aucun store neuf). La mutation de statut est
//   portée par l'agrégat CompteUtilisateur (Domain pur : méthode Activer()). Le changement est
//   persisté (relu depuis le store réel), et AUCUNE autre caractéristique (email, ActeurId) ne bouge.
//   La durabilité sur store Mongo réel est prouvée séparément en Api.Tests (acceptation runtime).
public class Scenario1_ActiverCompte
{
    private const string CompteId = "compte-alice-abc";
    private const string Email = "alice@foyer.fr";
    private const string ActeurId = "acteur-alice";

    // ---------- Acceptation (boucle externe, frontière Application, store réel) ----------
    // Traduit le scénario Gherkin : un CompteUtilisateur existant de statut « Inactif » est activé ;
    // le store réel (référentiel de comptes) doit ensuite l'énumérer avec le statut « Actif », le
    // changement persisté (relu depuis le store), l'email et l'ActeurId INCHANGÉS.
    [Fact]
    public void Acceptation_Should_Faire_passer_le_statut_a_Actif_persiste_sans_toucher_email_ni_acteur_When_on_active_un_compte_Inactif()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        referentiel.Creer(CompteId, Email, StatutCompte.Inactif, ActeurId);
        var handler = new ActiverCompteHandler(referentiel);

        var resultat = handler.Handle(new ActiverCompteCommand(CompteId));

        Assert.True(resultat.EstSucces);
        var compte = referentiel.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.Equal(StatutCompte.Actif, compte.Statut);   // le statut devient « Actif »
        Assert.Equal(Email, compte.Email);                 // ... email inchangé
        Assert.Equal(ActeurId, compte.ActeurId);           // ... acteur associé inchangé
    }

    // ---------- Test #1 — Driver : l'activation fait basculer le statut à Actif ----------
    // Contradiction : aucune commande/handler ActiverCompte n'existe — le référentiel de comptes n'a
    // pas de chemin d'activation. Force l'orchestration : l'activation cible le compte par son id et
    // persiste le statut « Actif » via le port d'écriture, de sorte qu'il soit relu « Actif ».
    [Fact]
    public void Should_Faire_passer_le_statut_a_Actif_When_on_active_un_compte_Inactif()
    {
        var referentiel = new FakeReferentielComptes();
        referentiel.Creer(CompteId, Email, StatutCompte.Inactif, ActeurId);
        var handler = new ActiverCompteHandler(referentiel);

        var resultat = handler.Handle(new ActiverCompteCommand(CompteId));

        Assert.True(resultat.EstSucces);
        var compte = referentiel.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.Equal(StatutCompte.Actif, compte.Statut);
    }

    // ---------- Test #2 — Driver : l'activation ne touche pas email ni acteur associé ----------
    // Contradiction : une impl minimale pourrait ré-écrire le compte en repartant de zéro (email vide,
    // acteur perdu). Force la préservation des autres caractéristiques (mutation ciblée du seul statut).
    [Fact]
    public void Should_Preserver_email_et_acteur_associe_When_on_active_un_compte_Inactif()
    {
        var referentiel = new FakeReferentielComptes();
        referentiel.Creer(CompteId, Email, StatutCompte.Inactif, ActeurId);
        var handler = new ActiverCompteHandler(referentiel);

        handler.Handle(new ActiverCompteCommand(CompteId));

        var compte = referentiel.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.Equal(Email, compte.Email);
        Assert.Equal(ActeurId, compte.ActeurId);
    }
}
