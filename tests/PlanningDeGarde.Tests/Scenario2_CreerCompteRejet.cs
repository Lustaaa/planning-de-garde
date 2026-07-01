using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.2 — Rejet : email vide ou email en doublon (@back)
//   Tranche BACKEND (frontière Application) : la commande CreerCompte échoue AVANT toute écriture si
//   l'email est vide (email requis) OU s'il est déjà porté par un compte (email déjà utilisé). Dans
//   les deux cas, le référentiel reste inchangé (aucun compte vide ni doublon persisté).
public class Scenario2_CreerCompteRejet
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurId = "acteur-alice";
    private const string AutreActeurId = "acteur-bob";

    // ---------- Acceptation (boucle externe, frontière Application) — email en doublon ----------
    // Un référentiel contient déjà un compte « alice@foyer.fr » ; tenter d'en créer un second avec le
    // même email échoue avec un motif clair et laisse le référentiel STRICTEMENT inchangé (toujours
    // exactement un compte « alice@foyer.fr », porté par le même id).
    [Fact]
    public void Acceptation_Should_Echouer_et_laisser_le_referentiel_inchange_When_l_email_est_deja_utilise()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteHandler(referentiel, referentiel);
        var premier = handler.Handle(new CreerCompteCommand(Email, ActeurId));
        Assert.True(premier.EstSucces);

        var doublon = handler.Handle(new CreerCompteCommand(Email, AutreActeurId));

        Assert.False(doublon.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(doublon.Motif)); // motif clair
        var comptes = referentiel.EnumererComptes();
        Assert.Single(comptes, c => c.Email == Email);          // toujours EXACTEMENT un compte alice
        Assert.Equal(premier.Valeur!.CompteId, comptes.Single(c => c.Email == Email).Id); // le premier, intact
    }

    // ---------- Acceptation — email vide ----------
    // Tenter de créer un compte d'email vide échoue avec un motif clair et n'écrit RIEN.
    [Fact]
    public void Acceptation_Should_Echouer_et_ne_rien_ecrire_When_l_email_est_vide()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerCompteCommand("", ActeurId));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(referentiel.EnumererComptes()); // aucun compte vide persisté
    }

    // ---------- Test #1 — Driver : email vide refusé, aucune écriture ----------
    // Contradiction : le handler écrit toujours (Sc.1). Force une garde « email requis » AVANT toute
    // génération d'id et toute écriture.
    [Fact]
    public void Should_Refuser_sans_ecrire_When_l_email_est_vide()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerCompteCommand("", ActeurId));

        Assert.False(resultat.EstSucces);
        Assert.Empty(referentiel.EnumererComptes());
    }

    // ---------- Test #2 — Driver : email tout-espaces refusé, aucune écriture ----------
    // Contradiction : la garde du #1 pourrait se limiter à la chaîne strictement vide. Force le rejet
    // d'un email tout-espaces (whitespace) — email requis = non blanc.
    [Fact]
    public void Should_Refuser_sans_ecrire_When_l_email_est_tout_espaces()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerCompteCommand("   ", ActeurId));

        Assert.False(resultat.EstSucces);
        Assert.Empty(referentiel.EnumererComptes());
    }

    // ---------- Test #3 — Driver : email en doublon refusé, aucune écriture supplémentaire ----------
    // Contradiction : rien n'empêche deux comptes de même email. Force une garde « email déjà utilisé »,
    // lue sur le référentiel courant (unicité de l'email).
    [Fact]
    public void Should_Refuser_le_second_compte_sans_ecrire_When_l_email_est_deja_utilise()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel);
        Assert.True(handler.Handle(new CreerCompteCommand(Email, ActeurId)).EstSucces);

        var doublon = handler.Handle(new CreerCompteCommand(Email, AutreActeurId));

        Assert.False(doublon.EstSucces);
        Assert.Single(referentiel.EnumererComptes(), c => c.Email == Email); // pas de second compte
        Assert.Single(referentiel.EnumererComptes());                        // référentiel inchangé
    }
}
