using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.9 — Création de compte LIBRE-SERVICE : email neuf + mot de passe → compte Inactif (@back, Volet 3)
//   Tranche BACKEND (frontière Application) : un visiteur « qui n'a pas encore de compte » s'inscrit
//   lui-même avec un email neuf + un mot de passe. Distinct de la création admin s22 (CreerCompteHandler,
//   qui exige un acteur DÉCLARÉ) : ici PAS d'acteur — ActeurId reste NULL (association/activation
//   ultérieures s22/s24). Le compte naît Inactif (défaut s22) avec le mot de passe HACHÉ (IHacheurMotDePasse
//   s25). Gardes s22 étendues : email unique (Sc.10 pour le rejet) + mot de passe REQUIS.
public class Scenario9_CreerCompteLibreService
{
    private const string Email = "nouveau@foyer.fr";
    private const string MotDePasse = "s3cr3t-nouveau";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Aucun compte pour l'email : l'inscription libre-service crée un compte Inactif, mot de passe HACHÉ
    // (jamais le clair), ActeurId NULL (pas d'acteur), énuméré exactement une fois par son email.
    [Fact]
    public void Acceptation_Should_Creer_un_compte_Inactif_sans_acteur_avec_mot_de_passe_hache_When_un_visiteur_s_inscrit_en_libre_service()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteLibreServiceHandler(comptes, comptes, hacheur);

        var resultat = handler.Handle(new CreerCompteLibreServiceCommand(Email, MotDePasse));

        Assert.True(resultat.EstSucces);
        var compte = comptes.EnumererComptes().Single(c => c.Email == Email);
        Assert.Equal(StatutCompte.Inactif, compte.Statut);          // naît Inactif (défaut s22)
        Assert.Null(compte.ActeurId);                               // ActeurId nullable : pas d'acteur
        Assert.NotNull(compte.MotDePasseHache);
        Assert.NotEqual(MotDePasse, compte.MotDePasseHache);        // mot de passe HACHÉ, jamais le clair
        Assert.True(hacheur.Verifier(MotDePasse, compte.MotDePasseHache!)); // condensat vérifiable
    }

    // ---------- Driver — le mot de passe est REQUIS (garde s22 étendue) ----------
    // Contradiction : sans garde, un mot de passe vide/tout-espaces créerait un compte sans facteur
    // d'authentification. Force le rejet AVANT toute écriture — aucun compte persisté.
    [Fact]
    public void Should_Rejeter_sans_ecriture_When_le_mot_de_passe_est_vide()
    {
        var comptes = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteLibreServiceHandler(comptes, comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new CreerCompteLibreServiceCommand(Email, "   "));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));    // motif clair
        Assert.Empty(comptes.EnumererComptes());                    // aucune écriture
    }

    // ---------- Driver — l'email est REQUIS (garde s22 étendue) ----------
    // Contradiction : un email vide créerait un compte non résolvable. Force le rejet avant écriture.
    [Fact]
    public void Should_Rejeter_sans_ecriture_When_l_email_est_vide()
    {
        var comptes = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteLibreServiceHandler(comptes, comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new CreerCompteLibreServiceCommand("  ", MotDePasse));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(comptes.EnumererComptes());
    }

    // ---------- Driver — l'identifiant du compte est OPAQUE, distinct de l'email (anti-pattern s06) ----------
    // Contradiction : un raccourci « id = email » (corrigé s06) ré-émergerait. Force un id opaque généré.
    [Fact]
    public void Should_Porter_un_identifiant_opaque_distinct_de_l_email_When_un_compte_libre_service_est_cree()
    {
        var comptes = new ReferentielComptesEnMemoire();
        var handler = new CreerCompteLibreServiceHandler(comptes, comptes, new HacheurMotDePassePbkdf2());

        var compteId = handler.Handle(new CreerCompteLibreServiceCommand(Email, MotDePasse)).Valeur!.CompteId;

        Assert.NotEqual(Email, compteId);
    }
}
