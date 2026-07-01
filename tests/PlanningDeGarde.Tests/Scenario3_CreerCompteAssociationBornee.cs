using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.3 — Association bornée 1-1 (acteur déclaré, au plus un compte) (@back)
//   Tranche BACKEND (frontière Application) : la commande CreerCompte échoue AVANT toute écriture si
//   l'acteur porte DÉJÀ un compte (un acteur ne porte qu'un seul compte) OU si l'id d'acteur est
//   ABSENT du foyer (acteur inconnu). Dans les deux cas, l'énumération des comptes reste inchangée.
public class Scenario3_CreerCompteAssociationBornee
{
    private const string ActeurDeclare = "acteur-alice";
    private const string AutreActeurDeclare = "acteur-bob";
    private const string ActeurInconnu = "acteur-fantome";

    private static CreerCompteHandler Handler(IEnumerationComptes comptes, IEditeurComptes editeur)
        => new(comptes, editeur, new FakeEnumerationActeursFoyer(ActeurDeclare, AutreActeurDeclare));

    // ---------- Acceptation — un acteur ne porte qu'un seul compte ----------
    // Un acteur déclaré porte déjà un compte ; tenter d'en créer un second (email distinct valide)
    // pour ce même acteur échoue avec un motif clair et laisse l'énumération STRICTEMENT inchangée.
    [Fact]
    public void Acceptation_Should_Echouer_et_laisser_l_enumeration_inchangee_When_l_acteur_porte_deja_un_compte()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = Handler(referentiel, referentiel);
        var premier = handler.Handle(new CreerCompteCommand("alice@foyer.fr", ActeurDeclare));
        Assert.True(premier.EstSucces);

        var second = handler.Handle(new CreerCompteCommand("alice.pro@foyer.fr", ActeurDeclare));

        Assert.False(second.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(second.Motif));
        var comptes = referentiel.EnumererComptes();
        Assert.Single(comptes, c => c.ActeurId == ActeurDeclare); // toujours un seul compte pour l'acteur
        Assert.Single(comptes);                                   // énumération inchangée (le premier seul)
    }

    // ---------- Acceptation — acteur inconnu du foyer ----------
    // Tenter de créer un compte pour un id d'acteur ABSENT du foyer échoue avec un motif clair et
    // n'écrit RIEN (l'énumération des comptes reste vide).
    [Fact]
    public void Acceptation_Should_Echouer_et_ne_rien_ecrire_When_l_acteur_est_inconnu_du_foyer()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = Handler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerCompteCommand("fantome@foyer.fr", ActeurInconnu));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(referentiel.EnumererComptes()); // aucun compte pour un acteur inconnu
    }

    // ---------- Test #1 — Driver : acteur inconnu refusé, aucune écriture ----------
    // Contradiction : le handler écrit dès que l'email est valide (Sc.1/Sc.2). Force une garde
    // « acteur inconnu » (id absent de IEnumerationActeursFoyer) AVANT toute écriture.
    [Fact]
    public void Should_Refuser_sans_ecrire_When_l_acteur_est_inconnu()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = Handler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerCompteCommand("fantome@foyer.fr", ActeurInconnu));

        Assert.False(resultat.EstSucces);
        Assert.Empty(referentiel.EnumererComptes());
    }

    // ---------- Test #2 — Driver : second compte pour le même acteur refusé, aucune écriture ----------
    // Contradiction : rien n'empêche un acteur de porter deux comptes. Force une garde « acteur déjà
    // porteur d'un compte » (unicité 1-1 côté acteur), lue sur le référentiel courant.
    [Fact]
    public void Should_Refuser_le_second_compte_sans_ecrire_When_l_acteur_porte_deja_un_compte()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = Handler(referentiel, referentiel);
        Assert.True(handler.Handle(new CreerCompteCommand("alice@foyer.fr", ActeurDeclare)).EstSucces);

        var second = handler.Handle(new CreerCompteCommand("alice.pro@foyer.fr", ActeurDeclare));

        Assert.False(second.EstSucces);
        Assert.Single(referentiel.EnumererComptes(), c => c.ActeurId == ActeurDeclare);
        Assert.Single(referentiel.EnumererComptes());
    }

    // ---------- Test #3 — Driver : un acteur déclaré DISTINCT reste créable (garde ni trop large) ----------
    // Contradiction : la garde du #2 pourrait interdire tout second compte quel que soit l'acteur.
    // Force que l'unicité 1-1 borne l'ACTEUR, pas le référentiel : un autre acteur déclaré, sans
    // compte, doit pouvoir en recevoir un.
    [Fact]
    public void Should_Autoriser_un_compte_pour_un_autre_acteur_declare_sans_compte_When_un_premier_acteur_porte_deja_un_compte()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = Handler(referentiel, referentiel);
        Assert.True(handler.Handle(new CreerCompteCommand("alice@foyer.fr", ActeurDeclare)).EstSucces);

        var second = handler.Handle(new CreerCompteCommand("bob@foyer.fr", AutreActeurDeclare));

        Assert.True(second.EstSucces);
        Assert.Single(referentiel.EnumererComptes(), c => c.ActeurId == AutreActeurDeclare);
    }
}
