using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.4 — Affecter un rôle (du référentiel) à un acteur, champ borné (@back)
//   Tranche BACKEND (frontière Application) : commande/handler AffecterRoleActeur qui écrit l'id de
//   rôle sur l'acteur via le chemin d'écriture de la config acteur (augmenté d'un id de rôle), MAIS
//   uniquement si l'id de rôle provient du référentiel (IEnumerationRoles). Un id de rôle hors
//   référentiel est REJETÉ sans écriture (l'acteur conserve son rôle précédent — jamais de rôle en
//   dur). On NE teste PAS ici de rendu Blazor.
public class Scenario4_AffecterRoleActeurBorne
{
    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin : un acteur déclaré + un référentiel contenant « Nounou ». Affecter
    // « Nounou » à l'acteur le fait porter cet id de rôle (persisté sur la config acteur). Puis tenter
    // d'affecter un id de rôle absent du référentiel est rejeté, et l'acteur conserve « Nounou ».
    [Fact]
    public void Acceptation_Should_Porter_l_id_de_role_du_referentiel_puis_rejeter_un_id_hors_referentiel_en_conservant_le_role_precedent_When_on_affecte_puis_tente_un_role_inconnu()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        var idNounou = new CreerRoleHandler(referentiel, referentiel).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
        var config = new ConfigurationFoyerEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        var handler = new AffecterRoleActeurHandler(referentiel, config);

        var affectation = handler.Handle(new AffecterRoleActeurCommand(acteurId, idNounou));
        Assert.True(affectation.EstSucces);
        Assert.Equal(idNounou, config.RoleDe(acteurId)); // l'acteur porte l'id de rôle « Nounou » (persisté)

        var horsReferentiel = handler.Handle(new AffecterRoleActeurCommand(acteurId, "role-inexistant"));
        Assert.False(horsReferentiel.EstSucces);         // rejet : valeur hors référentiel
        Assert.Equal("rôle hors référentiel", horsReferentiel.Motif);
        Assert.Equal(idNounou, config.RoleDe(acteurId)); // l'acteur conserve son rôle précédent (aucune écriture)
    }

    // ---------- Test #1 — Driver : affecter un rôle du référentiel écrit l'id de rôle sur l'acteur ----------
    // Contradiction : aucun handler AffecterRoleActeur n'existe — la config acteur ne porte pas de rôle.
    // Force l'orchestration : l'affectation écrit l'id de rôle sur l'acteur via le port d'écriture, de
    // sorte que RoleDe(acteur) = idNounou.
    [Fact]
    public void Should_Ecrire_l_id_de_role_sur_l_acteur_When_on_affecte_un_role_present_dans_le_referentiel()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-nounou", "Nounou");
        var config = new FakeConfigurationFoyer(new System.Collections.Generic.Dictionary<string, string>());
        var handler = new AffecterRoleActeurHandler(referentiel, config);

        var resultat = handler.Handle(new AffecterRoleActeurCommand("acteur-1", "role-nounou"));

        Assert.True(resultat.EstSucces);
        Assert.Equal("role-nounou", config.RoleDe("acteur-1")); // id de rôle écrit sur l'acteur
    }

    // ---------- Test #2 — Driver : affecter un rôle hors référentiel est rejeté sans écriture ----------
    // Contradiction : l'impl du #1 écrit inconditionnellement. Force la borne dure « rôle hors
    // référentiel » lue sur IEnumerationRoles — aucun rôle écrit, l'acteur reste sans rôle.
    [Fact]
    public void Should_Rejeter_l_affectation_sans_ecrire_de_role_When_l_id_de_role_est_absent_du_referentiel()
    {
        var referentiel = new FakeReferentielRoles(); // référentiel vide
        var config = new FakeConfigurationFoyer(new System.Collections.Generic.Dictionary<string, string>());
        var handler = new AffecterRoleActeurHandler(referentiel, config);

        var resultat = handler.Handle(new AffecterRoleActeurCommand("acteur-1", "role-inconnu"));

        Assert.False(resultat.EstSucces);
        Assert.Equal("rôle hors référentiel", resultat.Motif);
        Assert.Null(config.RoleDe("acteur-1")); // aucun rôle écrit (jamais de rôle en dur)
    }
}
