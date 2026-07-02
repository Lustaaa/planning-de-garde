using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.8 — Login : MAUVAIS mot de passe → refus, aucune session, motif NEUTRE (@back, Volet 3)
//   Tranche BACKEND (frontière Application) : sur un compte ACTIF avec mot de passe défini (haché), se
//   connecter avec le BON email mais un MAUVAIS mot de passe doit ÉCHOUER — aucune session ouverte. Le
//   motif est CLAIR mais NEUTRE : il ne distingue pas « email inconnu » (Sc.2) de « mauvais mot de passe »
//   (anti-énumération — un attaquant ne peut pas déduire qu'un email existe). C'est CE scénario qui force
//   la branche vérification+refus du handler (Verifier→Echec) via le hacheur injecté à Sc.7.
public class Scenario8_SeConnecterMauvaisMotDePasse
{
    private const string Email = "carole@foyer.fr";
    private const string ActeurCarole = "acteur-carole";
    private const string BonMotDePasse = "s3cr3t-carole";
    private const string MauvaisMotDePasse = "mauvais-mot-de-passe";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Bon email, MAUVAIS mot de passe sur un compte Actif : la commande doit ÉCHOUER, sans session ouverte.
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_le_mot_de_passe_est_mauvais()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, ActeurCarole, hacheur.Hacher(BonMotDePasse));
        var handler = new SeConnecterHandler(comptes, hacheur);

        var resultat = handler.Handle(new SeConnecterCommand(Email, MauvaisMotDePasse));

        Assert.False(resultat.EstSucces);                        // refus
        Assert.Null(resultat.Valeur);                            // aucune session ouverte
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair présent
    }

    // ---------- Driver — motif NEUTRE : mauvais mot de passe == email inconnu (anti-énumération) ----------
    // Contradiction : un motif « mauvais mot de passe » distinct de « email inconnu » (Sc.2) permettrait
    // d'énumérer les emails existants (l'un révèle que l'email existe, l'autre non). Force un motif
    // IDENTIQUE pour les deux refus — aucune fuite sur l'existence du compte.
    [Fact]
    public void Should_Retourner_le_meme_motif_que_l_email_inconnu_When_le_mot_de_passe_est_mauvais()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, ActeurCarole, hacheur.Hacher(BonMotDePasse));
        var handler = new SeConnecterHandler(comptes, hacheur);

        var refusMauvaisMdp = handler.Handle(new SeConnecterCommand(Email, MauvaisMotDePasse));
        var refusEmailInconnu = handler.Handle(new SeConnecterCommand("personne@foyer.fr", "peu-importe"));

        Assert.Equal(refusEmailInconnu.Motif, refusMauvaisMdp.Motif); // même motif : anti-énumération
    }
}
