using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.15 — Callback OAuth : identité inconnue OU compte Inactif → refus (@back @preuve-doublure)
//   PREUVE PAR DOUBLURE D'ADAPTATEUR (FakeFournisseurOAuth) ; câblage provider réel vérifié MANUELLEMENT
//   au G3. Tranche BACKEND (frontière Application) : le callback OAuth est REFUSÉ, sans aucune session,
//   dans trois cas — (1) le provider ne restitue AUCUNE identité (callback non exploitable), (2) l'identité
//   externe n'est liée à AUCUN compte (email inconnu), (3) l'identité est liée à un compte INACTIF. Le
//   motif est clair et cohérent avec les refus de la connexion locale s23/s24 (email inconnu / non activé).
public class Scenario15_CallbackOAuthRefus
{
    private const string EmailInconnu = "personne@foyer.fr";
    private const string EmailInactif = "bob@foyer.fr";

    private static ConnexionOAuthHandler Handler(ReferentielComptesEnMemoire comptes, IdentiteExterne? identite)
        => new(new FakeFournisseurOAuth(identite), new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()));

    // ---------- Acceptation (boucle externe) — identité NON RÉSOLUE par le provider ----------
    // Le provider ne restitue aucune identité (callback non exploitable) : refus, aucune session.
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_le_provider_ne_restitue_aucune_identite()
    {
        var comptes = new ReferentielComptesEnMemoire();
        var handler = Handler(comptes, identite: null); // aucune identité restituée

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-sans-identite"));

        Assert.False(resultat.EstSucces);                        // refus
        Assert.Null(resultat.Valeur);                            // aucune session
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair
    }

    // ---------- Acceptation — identité liée à AUCUN compte (email inconnu) ----------
    // L'identité externe porte un email qu'aucun compte ne porte : refus cohérent avec « email inconnu » s23.
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_l_identite_externe_n_est_liee_a_aucun_compte()
    {
        var comptes = new ReferentielComptesEnMemoire(); // référentiel vide
        var handler = Handler(comptes, new IdentiteExterne(EmailInconnu));

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-inconnu"));

        Assert.False(resultat.EstSucces);
        Assert.Null(resultat.Valeur);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
    }

    // ---------- Acceptation — identité liée à un compte INACTIF ----------
    // L'email de l'identité externe est porté par un compte INACTIF : refus cohérent avec « non activé » s23/s24.
    [Fact]
    public void Acceptation_Should_Refuser_sans_session_When_le_compte_lie_est_inactif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-bob", EmailInactif, StatutCompte.Inactif, "acteur-bob");
        var handler = Handler(comptes, new IdentiteExterne(EmailInactif));

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-inactif"));

        Assert.False(resultat.EstSucces);
        Assert.Null(resultat.Valeur);
        Assert.Contains("activ", resultat.Motif); // motif cohérent avec le refus « compte non activé » s23/s24
    }
}
