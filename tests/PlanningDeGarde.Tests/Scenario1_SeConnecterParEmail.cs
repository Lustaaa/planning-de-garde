using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.1 — Connexion locale par email d'un compte Actif (@back)
//   Tranche BACKEND (frontière Application) : commande/handler SeConnecter qui, sur l'email d'un compte
//   ACTIF existant (référentiel de comptes s22, store réel InMemory), OUVRE une session serveur dont
//   l'IDENTITÉ RÉELLE est l'acteur lié 1-1 au compte (id stable, s22). L'identité EFFECTIVE résout comme
//   aujourd'hui AU-DESSUS de cette identité réelle : sans incarnation, effective = réelle (s14 non
//   contournée). On NE teste PAS ici de rendu Blazor ni la session HTTP d'hôte (prouvée séparément en
//   runtime). Les REJETS (email inconnu / compte inactif) sont les Sc.2/Sc.3 — hors périmètre ici.
public class Scenario1_SeConnecterParEmail
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurAlice = "acteur-alice";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un visiteur se connecte avec l'email d'un compte ACTIF lié à l'acteur « Alice » ; la commande doit
    // RÉUSSIR, OUVRIR une session, et l'identité RÉELLE de la session doit être l'acteur « Alice »
    // (id stable du compte). L'identité EFFECTIVE, sans incarnation, résout sur la réelle.
    [Fact]
    public void Acceptation_Should_Ouvrir_une_session_dont_l_identite_reelle_est_l_acteur_lie_au_compte_When_on_se_connecte_avec_l_email_d_un_compte_actif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-1", Email, StatutCompte.Actif, ActeurAlice);
        var handler = new SeConnecterHandler(comptes);

        var resultat = handler.Handle(new SeConnecterCommand(Email));

        Assert.True(resultat.EstSucces);
        var session = resultat.Valeur!;
        Assert.Equal(ActeurAlice, session.IdentiteReelle);   // identité réelle = l'acteur lié au compte
        Assert.Equal(ActeurAlice, session.IdentiteEffective); // sans incarnation, effective = réelle (s14)
    }
}
