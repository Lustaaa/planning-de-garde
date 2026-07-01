using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.6 — Supprimer un rôle référencé : repli neutre + idempotence (@back)
//   Tranche BACKEND (frontière Application) : commande/handler SupprimerRole qui retire le rôle du
//   référentiel ET fait retomber « sans rôle » les acteurs qui le portaient (repli neutre, aucun rôle
//   fantôme, miroir du repli acteur orphelin s13/s19). Idempotente : supprimer un rôle absent est un
//   no-op qui réussit. La durabilité Mongo (relu après redémarrage) est prouvée en Api.Tests. On NE
//   teste PAS ici de rendu Blazor.
public class Scenario6_SupprimerRoleRepliNeutre
{
    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin : un référentiel contient « Nounou » et un acteur la porte. Supprimer
    // « Nounou » la retire du référentiel ET fait retomber l'acteur « sans rôle » (neutre). Supprimer un
    // rôle déjà absent est un no-op qui réussit (idempotence).
    [Fact]
    public void Acceptation_Should_Retirer_le_role_du_referentiel_faire_retomber_le_porteur_sans_role_et_etre_idempotent_When_le_parent_supprime_un_role_reference()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        var idNounou = new CreerRoleHandler(referentiel, referentiel).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
        var config = new ConfigurationFoyerEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        new AffecterRoleActeurHandler(referentiel, config).Handle(new AffecterRoleActeurCommand(acteurId, idNounou));
        var handler = new SupprimerRoleHandler(referentiel, config, config);

        var suppression = handler.Handle(new SupprimerRoleCommand(idNounou));

        Assert.True(suppression.EstSucces);
        Assert.DoesNotContain(referentiel.EnumererRoles(), r => r.Id == idNounou); // le rôle disparaît du référentiel
        Assert.Null(config.RoleDe(acteurId));                                       // le porteur retombe « sans rôle » (neutre)

        // Idempotence : supprimer le rôle déjà absent est un no-op qui réussit.
        var reSuppression = handler.Handle(new SupprimerRoleCommand(idNounou));
        Assert.True(reSuppression.EstSucces);
    }

    // ---------- Test #1 — Driver : la suppression retire le rôle du référentiel ----------
    // Contradiction : aucun handler SupprimerRole n'existe — le référentiel ne sait que créer/renommer.
    // Force l'orchestration : la suppression retire le rôle du référentiel (plus énuméré).
    [Fact]
    public void Should_Retirer_le_role_du_referentiel_When_le_parent_supprime_un_role()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-nounou", "Nounou");
        var acteurs = new FakeEnumerationActeursFoyer();
        var config = new FakeConfigurationFoyer(new System.Collections.Generic.Dictionary<string, string>());
        var handler = new SupprimerRoleHandler(referentiel, acteurs, config);

        var resultat = handler.Handle(new SupprimerRoleCommand("role-nounou"));

        Assert.True(resultat.EstSucces);
        Assert.Empty(referentiel.EnumererRoles()); // le rôle n'est plus énuméré
    }

    // ---------- Test #2 — Driver : les porteurs du rôle supprimé retombent « sans rôle » (repli neutre) ----------
    // Contradiction : l'impl du #1 ne touche pas les acteurs. Force le repli neutre : tout acteur portant
    // le rôle supprimé retombe « sans rôle » (RoleDe = null), aucun rôle fantôme.
    [Fact]
    public void Should_Faire_retomber_les_porteurs_sans_role_When_un_role_reference_est_supprime()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-nounou", "Nounou");
        var config = new ConfigurationFoyerEnMemoire(); // implémente énumération + RoleDe + RetirerRole
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        config.AffecterRole(acteurId, "role-nounou");
        var handler = new SupprimerRoleHandler(referentiel, config, config);

        handler.Handle(new SupprimerRoleCommand("role-nounou"));

        Assert.Null(config.RoleDe(acteurId)); // repli neutre : le porteur retombe « sans rôle »
    }

    // ---------- Test #3 — Driver : idempotence, supprimer un rôle absent réussit sans effet ----------
    // Contradiction : une impl qui lèverait sur clé absente casserait. Force la tolérance à l'absence :
    // supprimer un rôle jamais créé est un no-op qui réussit.
    [Fact]
    public void Should_Reussir_sans_effet_When_on_supprime_un_role_deja_absent()
    {
        var referentiel = new FakeReferentielRoles(); // référentiel vide
        var acteurs = new FakeEnumerationActeursFoyer();
        var config = new FakeConfigurationFoyer(new System.Collections.Generic.Dictionary<string, string>());
        var handler = new SupprimerRoleHandler(referentiel, acteurs, config);

        var resultat = handler.Handle(new SupprimerRoleCommand("role-inexistant"));

        Assert.True(resultat.EstSucces);            // no-op qui réussit (idempotence)
        Assert.Empty(referentiel.EnumererRoles());
    }
}
