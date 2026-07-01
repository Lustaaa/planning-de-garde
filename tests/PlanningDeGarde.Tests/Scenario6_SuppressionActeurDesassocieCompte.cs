using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.6 — Suppression concurrente de l'acteur associé : désassociation propre + idempotence (@back)
//   Miroir du repli acteur orphelin (s13/s19) : quand l'acteur associé est supprimé du foyer, le
//   compte retombe DÉSASSOCIÉ (ne référence plus d'acteur) — pas de compte fantôme pointant un acteur
//   absent. Repli PROPRE (pas rejet). Désassocier un compte déjà désassocié est un no-op qui réussit
//   (idempotence).
public class Scenario6_SuppressionActeurDesassocieCompte
{
    private const string Email = "alice@foyer.fr";

    // ---------- Acceptation — l'acteur supprimé désassocie son compte, qui reste énuméré ----------
    // Un acteur déclaré porte un compte ; quand on supprime l'acteur (store réel), le compte doit
    // retomber désassocié (ActeurId null), tout en RESTANT énuméré (le compte n'est pas détruit, juste
    // désassocié — pas de compte fantôme référençant un acteur absent).
    [Fact]
    public void Acceptation_Should_Faire_retomber_le_compte_desassocie_en_le_conservant_enumere_When_l_acteur_associe_est_supprime()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var comptes = new ReferentielComptesEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
        var compteId = new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand(Email, acteurId)).Valeur!.CompteId;

        var suppression = new SupprimerActeurHandler(config, new FakeNotificateurPlanning(), comptes, comptes)
            .Handle(new SupprimerActeurCommand(acteurId));

        Assert.True(suppression.EstSucces);
        var compte = comptes.EnumererComptes().Single(c => c.Id == compteId);
        Assert.Null(compte.ActeurId);                 // désassocié : ne référence plus l'acteur absent
        Assert.Equal(Email, compte.Email);            // le compte survit (pas détruit), email conservé
    }

    // ---------- Acceptation — pas de compte fantôme : aucun compte ne référence l'acteur supprimé ----------
    [Fact]
    public void Acceptation_Should_Ne_laisser_aucun_compte_referencant_l_acteur_absent_When_l_acteur_associe_est_supprime()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var comptes = new ReferentielComptesEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
        new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand(Email, acteurId));

        new SupprimerActeurHandler(config, new FakeNotificateurPlanning(), comptes, comptes)
            .Handle(new SupprimerActeurCommand(acteurId));

        Assert.DoesNotContain(comptes.EnumererComptes(), c => c.ActeurId == acteurId); // aucun fantôme
    }

    // ---------- Acceptation — idempotence : supprimer deux fois laisse le compte désassocié, réussit ----------
    // Désassocier un compte déjà désassocié est un no-op qui réussit : re-supprimer l'acteur (déjà
    // absent) ne rejette pas et conserve le compte désassocié.
    [Fact]
    public void Acceptation_Should_Rester_desassocie_et_reussir_When_on_supprime_l_acteur_une_seconde_fois()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var comptes = new ReferentielComptesEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
        var compteId = new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand(Email, acteurId)).Valeur!.CompteId;
        var handler = new SupprimerActeurHandler(config, new FakeNotificateurPlanning(), comptes, comptes);
        Assert.True(handler.Handle(new SupprimerActeurCommand(acteurId)).EstSucces);

        var second = handler.Handle(new SupprimerActeurCommand(acteurId)); // second passage = no-op

        Assert.True(second.EstSucces);
        Assert.Null(comptes.EnumererComptes().Single(c => c.Id == compteId).ActeurId);
    }

    // ---------- Driver — un compte pointant un AUTRE acteur n'est pas désassocié (repli ciblé) ----------
    // Contradiction : le repli pourrait désassocier tous les comptes. Force un repli CIBLÉ sur les
    // seuls comptes référençant l'acteur supprimé.
    [Fact]
    public void Should_Ne_pas_desassocier_les_comptes_d_autres_acteurs_When_un_acteur_est_supprime()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var comptes = new ReferentielComptesEnMemoire();
        var acteurA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
        var acteurB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob", "bleu")).Valeur!.ActeurId;
        new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand("alice@foyer.fr", acteurA));
        var compteB = new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand("bob@foyer.fr", acteurB)).Valeur!.CompteId;

        new SupprimerActeurHandler(config, new FakeNotificateurPlanning(), comptes, comptes)
            .Handle(new SupprimerActeurCommand(acteurA));

        Assert.Equal(acteurB, comptes.EnumererComptes().Single(c => c.Id == compteB).ActeurId); // B intact
    }
}
