using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.5 — Acteur sans rôle = neutre assumé (@back)
//   Tranche BACKEND (frontière Application) : l'attribut rôle est OPTIONNEL. Un acteur jamais affecté
//   est « sans rôle » (RoleDe = null), sans erreur ni rôle fantôme. Retirer le rôle d'un acteur qui en
//   portait un le ramène à « sans rôle » (neutre). On NE teste PAS ici de rendu Blazor.
public class Scenario5_ActeurSansRoleNeutre
{
    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin : un acteur déclaré sans affectation est « sans rôle » (neutre, aucune
    // erreur, aucun rôle fantôme) ; après avoir porté « Nounou », le retrait le ramène à « sans rôle ».
    [Fact]
    public void Acceptation_Should_Etre_sans_role_par_defaut_et_le_redevenir_apres_retrait_sans_erreur_ni_role_fantome_When_on_enumere_le_role_d_un_acteur_jamais_affecte_puis_retire()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        // Acteur jamais affecté → « sans rôle » (neutre), aucune erreur, aucun rôle fantôme inventé.
        Assert.Null(config.RoleDe(acteurId));

        // Il porte « Nounou », puis on le retire → ramené à « sans rôle » (neutre).
        var referentiel = new ReferentielRolesEnMemoire();
        var idNounou = new CreerRoleHandler(referentiel, referentiel).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
        new AffecterRoleActeurHandler(referentiel, config).Handle(new AffecterRoleActeurCommand(acteurId, idNounou));
        Assert.Equal(idNounou, config.RoleDe(acteurId));

        var retrait = new RetirerRoleActeurHandler(config).Handle(new RetirerRoleActeurCommand(acteurId));

        Assert.True(retrait.EstSucces);
        Assert.Null(config.RoleDe(acteurId)); // ramené à « sans rôle » (neutre)
    }

    // ---------- Test #1 — Driver : retirer le rôle ramène l'acteur à « sans rôle » ----------
    // Contradiction : aucun handler RetirerRoleActeur n'existe — une fois un rôle porté, rien ne le
    // retire. Force l'orchestration : le retrait remet l'attribut rôle à non renseigné (RoleDe = null).
    [Fact]
    public void Should_Ramener_l_acteur_a_sans_role_When_on_retire_le_role_d_un_acteur_qui_en_portait_un()
    {
        var config = new FakeConfigurationFoyer(new Dictionary<string, string>());
        config.AffecterRole("acteur-1", "role-nounou");
        var handler = new RetirerRoleActeurHandler(config);

        var resultat = handler.Handle(new RetirerRoleActeurCommand("acteur-1"));

        Assert.True(resultat.EstSucces);
        Assert.Null(config.RoleDe("acteur-1")); // « sans rôle » (neutre) après retrait
    }
}
