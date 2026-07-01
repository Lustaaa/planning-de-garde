using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.1 — Créer un compte utilisateur associé à un acteur déclaré (@back)
//   Tranche BACKEND (frontière Application) : commande/handler CreerCompte qui génère un IDENTIFIANT
//   STABLE NEUF OPAQUE (jamais dérivé de l'email), porte l'email, le statut « inactif » par défaut et
//   l'id stable de l'acteur associé (association 1-1), persiste le compte via le port d'écriture
//   IEditeurComptes, et l'expose à l'énumération IEnumerationComptes EXACTEMENT UNE FOIS.
//   La durabilité sur store Mongo réel (survit au redémarrage) est prouvée séparément en Api.Tests
//   (acceptation runtime obligatoire). On NE teste PAS ici de rendu Blazor.
public class Scenario1_CreerCompte
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurId = "acteur-alice";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin à la frontière Application (sans IHM) : un parent crée un compte
    // d'email « alice@foyer.fr » pour un acteur déclaré ; le store réel (référentiel de comptes) doit
    // ensuite l'ÉNUMÉRER EXACTEMENT UNE FOIS, porté par un identifiant NEUF OPAQUE (≠ email), avec le
    // statut « inactif » par défaut et l'id stable de l'acteur associé (association 1-1).
    [Fact]
    public void Acceptation_Should_Enumerer_le_compte_exactement_une_fois_sur_un_id_neuf_opaque_email_statut_inactif_et_acteur_associe_When_le_parent_cree_un_compte()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteHandler(referentiel, referentiel, new FakeEnumerationActeursFoyer(ActeurId));

        var resultat = handler.Handle(new CreerCompteCommand(Email, ActeurId));

        Assert.True(resultat.EstSucces);
        var compteId = resultat.Valeur!.CompteId;
        var comptes = referentiel.EnumererComptes();
        Assert.Single(comptes, c => c.Email == Email);           // le compte énuméré EXACTEMENT une fois
        var compte = comptes.Single(c => c.Email == Email);
        Assert.Equal(compteId, compte.Id);                       // ... porté par l'id neuf retourné
        Assert.NotEqual(Email, compteId);                        // ... identifiant opaque, jamais l'email
        Assert.Equal(StatutCompte.Inactif, compte.Statut);       // ... statut « inactif » par défaut
        Assert.Equal(ActeurId, compte.ActeurId);                 // ... association 1-1 à l'acteur déclaré
    }

    // ---------- Test #1 — Driver : une création fait EXISTER le compte, résolu par son email sur un id neuf ----------
    // Contradiction : aucune commande/handler CreerCompte n'existe — le référentiel de comptes n'a pas
    // de chemin d'écriture. Force l'orchestration : une création génère un identifiant et persiste le
    // compte via le port d'écriture, de sorte qu'il soit énuméré avec l'email « alice@foyer.fr » sur l'id.
    [Fact]
    public void Should_Faire_exister_le_compte_cree_resolu_par_son_email_sur_un_identifiant_neuf_When_le_parent_cree_un_compte()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel, new FakeEnumerationActeursFoyer(ActeurId));

        var resultat = handler.Handle(new CreerCompteCommand(Email, ActeurId));

        Assert.True(resultat.EstSucces);
        var idNeuf = resultat.Valeur!.CompteId;
        var compte = referentiel.EnumererComptes().Single(c => c.Id == idNeuf);
        Assert.Equal(Email, compte.Email); // le compte créé est résolu par son email sur l'id généré
    }

    // ---------- Test #2 — Driver : l'identifiant est OPAQUE, distinct de l'email ----------
    // Contradiction : l'impl minimale du #1 pourrait prendre le raccourci « id = email »
    // (email-comme-identité, anti-pattern corrigé au s06). Force un identifiant OPAQUE généré, ≠ email.
    [Fact]
    public void Should_Porter_un_identifiant_opaque_distinct_de_l_email_When_un_compte_est_cree()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel, new FakeEnumerationActeursFoyer(ActeurId));

        var idNeuf = handler.Handle(new CreerCompteCommand(Email, ActeurId)).Valeur!.CompteId;

        Assert.NotEqual(Email, idNeuf); // identifiant opaque, jamais l'email (anti-pattern s06)
    }

    // ---------- Test #3 — Driver : le statut par défaut est « inactif » ----------
    // Contradiction : rien ne force le statut initial. Force le défaut métier « inactif » (l'activation
    // viendra avec la prise en main de compte, palier 13).
    [Fact]
    public void Should_Porter_le_statut_inactif_par_defaut_When_un_compte_est_cree()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel, new FakeEnumerationActeursFoyer(ActeurId));

        var idNeuf = handler.Handle(new CreerCompteCommand(Email, ActeurId)).Valeur!.CompteId;

        var compte = referentiel.EnumererComptes().Single(c => c.Id == idNeuf);
        Assert.Equal(StatutCompte.Inactif, compte.Statut);
    }

    // ---------- Test #4 — Driver : le compte référence l'id stable de l'acteur (association 1-1) ----------
    // Contradiction : rien ne force le portage de l'acteur associé. Force la relation identité↔acteur.
    [Fact]
    public void Should_Referencer_l_id_stable_de_l_acteur_associe_When_un_compte_est_cree()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new CreerCompteHandler(referentiel, referentiel, new FakeEnumerationActeursFoyer(ActeurId));

        var idNeuf = handler.Handle(new CreerCompteCommand(Email, ActeurId)).Valeur!.CompteId;

        var compte = referentiel.EnumererComptes().Single(c => c.Id == idNeuf);
        Assert.Equal(ActeurId, compte.ActeurId);
    }
}
