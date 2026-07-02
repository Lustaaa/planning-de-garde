using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.7 — Login email + MOT DE PASSE : bon couple → session ouverte (@back, Volet 3)
//   Tranche BACKEND (frontière Application) : introduit un FACTEUR mot de passe distinct de l'email-only
//   s23. Le mot de passe est stocké HACHÉ sur CompteUtilisateur (jamais en clair) — hachage par un vrai
//   code prod (IHacheurMotDePasse, réalisé PBKDF2 en Infrastructure). Sur le bon couple email+mot de
//   passe d'un compte ACTIF, la commande RÉUSSIT et OUVRE une session dont l'identité réelle est l'acteur
//   lié 1-1 au compte (cf. Sc.5). Le mot de passe n'est JAMAIS retourné ni exposé par le canal.
//   Le REJET « mauvais mot de passe » est le Sc.8 (hors périmètre ici — on ne vole pas son rouge).
public class Scenario7_SeConnecterAvecMotDePasse
{
    private const string Email = "carole@foyer.fr";
    private const string ActeurCarole = "acteur-carole";
    private const string MotDePasse = "s3cr3t-carole";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un compte ACTIF avec un mot de passe DÉFINI (haché via le vrai hacheur) : se connecter avec le bon
    // email ET le bon mot de passe doit RÉUSSIR, ouvrir une session dont l'identité réelle = l'acteur lié
    // au compte. Le mot de passe (clair) ne doit apparaître nulle part dans la valeur retournée.
    [Fact]
    public void Acceptation_Should_Ouvrir_une_session_sur_l_acteur_du_compte_sans_exposer_le_mot_de_passe_When_le_couple_email_mot_de_passe_est_bon()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, ActeurCarole, hacheur.Hacher(MotDePasse));
        var handler = new SeConnecterHandler(comptes, hacheur);

        var resultat = handler.Handle(new SeConnecterCommand(Email, MotDePasse));

        Assert.True(resultat.EstSucces);
        var session = resultat.Valeur!;
        Assert.Equal(ActeurCarole, session.IdentiteReelle);    // identité réelle = l'acteur lié au compte
        Assert.Equal(ActeurCarole, session.IdentiteEffective); // sans incarnation, effective = réelle (s14)
        // Le mot de passe clair n'est JAMAIS exposé par le canal (la session ne le porte pas).
        Assert.DoesNotContain(MotDePasse, session.ToString());
    }

    // ---------- Driver — le mot de passe est stocké HACHÉ, jamais en clair ----------
    // Contradiction : un facteur mot de passe qui persisterait le clair fuirait le secret. Force un
    // hachage à l'écriture : le compte relu du référentiel ne porte pas le mot de passe en clair.
    [Fact]
    public void Should_Persister_le_mot_de_passe_hache_jamais_en_clair_When_un_compte_est_cree_avec_mot_de_passe()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, ActeurCarole, hacheur.Hacher(MotDePasse));

        var compte = comptes.EnumererComptes().Single(c => c.Email == Email);

        Assert.NotNull(compte.MotDePasseHache);
        Assert.NotEqual(MotDePasse, compte.MotDePasseHache); // haché, jamais le clair
    }
}
