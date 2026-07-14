using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 41 — Sc.3 — Désactiver un compte (Actif → Inactif, réutilise IEditeurComptes) (@back)
//   Sens OFF du toggle actif (débloque le verrou ON s33). La commande DesactiverCompte cible un compte
//   existant par son id stable opaque (s22) et fait passer son statut Actif→Inactif via le port
//   d'écriture IEditeurComptes (aucun nouvel agrégat, aucun store neuf). La mutation de statut est
//   portée par l'agrégat CompteUtilisateur (Domain pur : Desactiver()). No-op idempotent si déjà
//   Inactif, compte inconnu refusé sans mutation. Un compte redevenu Inactif refuse la connexion
//   (garde s23), le sens ON (activer, s24) reste inchangé.
public class Sprint41Sc3_DesactiverCompte
{
    private const string CompteId = "compte-alice-abc";
    private const string Email = "alice@foyer.fr";
    private const string ActeurId = "acteur-alice";
    private const string IdAbsent = "id-absent";

    // ---------- Acceptation (frontière Application, store réel) ----------
    // Un CompteUtilisateur « Actif » est désactivé ; le store réel doit ensuite l'énumérer « Inactif »,
    // le changement persisté, email et acteur associé INCHANGÉS.
    [Fact]
    public void Acceptation_Should_Faire_passer_le_statut_a_Inactif_persiste_sans_toucher_email_ni_acteur_When_on_desactive_un_compte_Actif()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        referentiel.Creer(CompteId, Email, StatutCompte.Actif, ActeurId);
        var handler = new DesactiverCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new DesactiverCompteCommand(CompteId));

        Assert.True(resultat.EstSucces);
        var compte = referentiel.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.Equal(StatutCompte.Inactif, compte.Statut); // le statut devient « Inactif »
        Assert.Equal(Email, compte.Email);                 // ... email inchangé
        Assert.Equal(ActeurId, compte.ActeurId);           // ... acteur associé inchangé
    }

    // ---------- Test #1 — Domain : Desactiver fait passer le statut à Inactif ----------
    // Contradiction : l'agrégat CompteUtilisateur n'a pas de chemin de désactivation. Force Desactiver()
    // à faire passer le statut à Inactif (mutation immuable ciblée).
    [Fact]
    public void Domain_Should_Faire_passer_le_statut_a_Inactif_When_on_desactive_un_compte_Actif()
    {
        var compte = new CompteUtilisateur(CompteId, Email, StatutCompte.Actif, ActeurId);

        var desactive = compte.Desactiver();

        Assert.Equal(StatutCompte.Inactif, desactive.Statut);
        Assert.Equal(Email, desactive.Email);     // seul le statut change
        Assert.Equal(ActeurId, desactive.ActeurId);
    }

    // ---------- Test #2 — Driver : idempotence — désactiver un compte déjà Inactif réussit (no-op) ----------
    [Fact]
    public void Should_Reussir_en_no_op_et_rester_Inactif_When_on_desactive_un_compte_deja_Inactif()
    {
        var referentiel = new FakeReferentielComptes();
        referentiel.Creer(CompteId, Email, StatutCompte.Inactif, ActeurId);
        var handler = new DesactiverCompteHandler(referentiel, referentiel);

        Assert.True(handler.Handle(new DesactiverCompteCommand(CompteId)).EstSucces);
        Assert.True(handler.Handle(new DesactiverCompteCommand(CompteId)).EstSucces); // deux fois de suite

        var comptes = referentiel.EnumererComptes().Where(c => c.Id == CompteId).ToList();
        Assert.Single(comptes);                                // aucun doublon
        Assert.Equal(StatutCompte.Inactif, comptes[0].Statut); // reste Inactif
    }

    // ---------- Test #3 — Driver : compte inconnu refusé sans mutation ----------
    [Fact]
    public void Should_Refuser_sans_mutation_When_le_compte_est_inconnu()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new DesactiverCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new DesactiverCompteCommand(IdAbsent));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif restitué
        Assert.Empty(referentiel.EnumererComptes());             // aucune mutation, aucun compte fantôme
    }

    // ---------- Non-régression — un compte redevenu Inactif refuse la connexion (garde s23) ----------
    // Le sens OFF réintroduit l'effet de la garde « compte non activé » : après désactivation, la
    // connexion par email échoue. Le sens ON (activer) n'est pas régressé (prouvé par les tests s24).
    [Fact]
    public void Should_Refuser_la_connexion_When_le_compte_a_ete_desactive()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        referentiel.Creer(CompteId, Email, StatutCompte.Actif, ActeurId);
        new DesactiverCompteHandler(referentiel, referentiel).Handle(new DesactiverCompteCommand(CompteId));

        var connexion = new SeConnecterHandler(referentiel, new HacheurMotDePassePbkdf2())
            .Handle(new SeConnecterCommand(Email));

        Assert.False(connexion.EstSucces);                  // le compte désactivé refuse la connexion
        Assert.Contains("activ", connexion.Motif);          // motif « compte non activé » (garde s23)
    }
}
