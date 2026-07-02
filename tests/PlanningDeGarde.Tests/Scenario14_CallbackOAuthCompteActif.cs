using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.14 — Callback OAuth, identité liée à un compte Actif → SessionOuverte (@back @preuve-doublure)
//   PREUVE PAR DOUBLURE D'ADAPTATEUR (FakeFournisseurOAuth) ; câblage provider réel (≥1, ex. Google)
//   vérifié MANUELLEMENT au G3. Tranche BACKEND (frontière Application) : le callback OAuth, résolu en
//   une identité externe (email vérifié par le provider) liée à un compte ACTIF, ouvre une SessionOuverte
//   dont l'identité réelle est l'acteur du compte (cf. Sc.5). Le chemin de session est le MÊME que la
//   connexion locale s23 (réutilise SeConnecterHandler) — AUCUN agrégat durable neuf.
public class Scenario14_CallbackOAuthCompteActif
{
    private const string Email = "carole@foyer.fr";
    private const string ActeurCarole = "acteur-carole";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Le fournisseur (doublure) restitue une identité externe pour « carole@foyer.fr », liée à un compte
    // ACTIF : le traitement du callback doit RÉUSSIR et ouvrir une session dont l'identité réelle ET
    // effective est l'acteur du compte (même résolution que s23/Sc.5).
    [Fact]
    public void Acceptation_Should_Ouvrir_une_session_sur_l_acteur_du_compte_When_le_callback_OAuth_restitue_une_identite_liee_a_un_compte_actif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, ActeurCarole); // compte OAuth : pas de mot de passe local
        var seConnecter = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2());
        var fournisseur = new FakeFournisseurOAuth(new IdentiteExterne(Email));
        var handler = new ConnexionOAuthHandler(fournisseur, seConnecter);

        var resultat = handler.Handle(new CallbackOAuthCommand("callback-google-abc"));

        Assert.True(resultat.EstSucces);
        var session = resultat.Valeur!;
        Assert.Equal(ActeurCarole, session.IdentiteReelle);    // identité réelle = l'acteur lié au compte (Sc.5)
        Assert.Equal(ActeurCarole, session.IdentiteEffective); // sans incarnation, effective = réelle (s14)
    }
}
