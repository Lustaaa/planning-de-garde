using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.4 — Acteur par défaut = utilisateur connecté (@back)
//   Tranche BACKEND (frontière Application) : côté serveur, on RÉSOUT l'acteur par défaut d'une session
//   ouverte = l'acteur lié 1-1 au compte connecté (relation s22). Cet acteur par défaut est celui exposé
//   au sélecteur (config/dialogs) — il provient de la SOURCE UNIQUE des acteurs du foyer
//   (IEnumerationActeursFoyer, convergence s20), jamais d'un libellé en dur. Le comportement NON connecté
//   (défaut actuel, pas de couplage au compte) est le Sc.6 — hors périmètre ici.
public class Scenario4_ActeurParDefautUtilisateurConnecte
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurAlice = "acteur-alice";
    private const string ActeurBob = "acteur-bob";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Une session ouverte pour « alice@foyer.fr » (lié à l'acteur « Alice ») : résoudre l'acteur par
    // défaut côté serveur doit rendre « Alice », ET « Alice » doit appartenir à la source unique des
    // acteurs du foyer (IEnumerationActeursFoyer) — l'acteur exposé au sélecteur est un acteur réel.
    [Fact]
    public void Acceptation_Should_Resoudre_l_acteur_lie_au_compte_connecte_et_present_dans_la_source_unique_When_une_session_est_ouverte()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-1", Email, StatutCompte.Actif, ActeurAlice);
        var acteurs = new FakeEnumerationActeursFoyer(ActeurAlice, ActeurBob);
        var session = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()).Handle(new SeConnecterCommand(Email)).Valeur!;
        var resolveur = new ResoudreActeurParDefautQuery();

        var acteurParDefaut = resolveur.Resoudre(session);

        Assert.Equal(ActeurAlice, acteurParDefaut);                         // l'acteur lié au compte connecté
        Assert.Contains(acteurParDefaut, acteurs.EnumererActeurs());        // ... présent dans la source unique s20
    }

    // ---------- Driver — le défaut suit le compte connecté (pas un acteur arbitraire) ----------
    // Contradiction : rendre « le premier acteur énuméré » satisferait l'acceptation par hasard (Alice
    // est en tête). Force le couplage au COMPTE : une session sur Bob rend Bob, pas la tête de liste.
    [Fact]
    public void Should_Rendre_l_acteur_du_compte_connecte_meme_s_il_n_est_pas_en_tete_de_liste_When_une_session_est_ouverte()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-bob", "bob@foyer.fr", StatutCompte.Actif, ActeurBob);
        _ = new FakeEnumerationActeursFoyer(ActeurAlice, ActeurBob); // Alice en tête, Bob connecté
        var session = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()).Handle(new SeConnecterCommand("bob@foyer.fr")).Valeur!;
        var resolveur = new ResoudreActeurParDefautQuery();

        var acteurParDefaut = resolveur.Resoudre(session);

        Assert.Equal(ActeurBob, acteurParDefaut); // le défaut suit le compte connecté, pas la tête de liste
    }
}
