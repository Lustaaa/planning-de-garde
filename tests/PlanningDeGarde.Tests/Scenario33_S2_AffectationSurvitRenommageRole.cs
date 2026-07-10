using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 33 — Sc.2 (early-green) — Éditer / ajouter un rôle : capacité déjà LIVRÉE s21
//   (CreerRoleHandler / RenommerRoleHandler, refus vide+doublon, durabilité Mongo). Le SM a retenu
//   l'option A (pas de handler neuf). Un SEUL point du Gherkin n'était asserté nulle part de façon
//   combinée : « l'affectation existante des acteurs au rôle reste COHÉRENTE après renommage
//   (renommage ≠ recréation) ». Ce test comble ce trou de couverture à la frontière Application,
//   sur les adaptateurs réels (référentiel de rôles + config acteur en mémoire) — il ne re-teste NI
//   la création, NI le renommage, NI les refus (déjà couverts s21).
public class Scenario33_S2_AffectationSurvitRenommageRole
{
    // ---------- Invariant : l'acteur affecté à un rôle reste affecté au MÊME rôle après renommage ----------
    // Le renommage mute le libellé SUR L'ID STABLE (jamais recréation) : l'acteur référence le rôle par
    // son id, donc son affectation survit intacte et résout désormais le nouveau libellé.
    [Fact]
    public void Should_Conserver_l_affectation_de_l_acteur_sur_le_meme_role_qui_resout_le_nouveau_libelle_When_le_role_affecte_est_renomme()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        var idNounou = new CreerRoleHandler(referentiel, referentiel).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;

        var config = new ConfigurationFoyerEnMemoire();
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        new AffecterRoleActeurHandler(referentiel, config).Handle(new AffecterRoleActeurCommand(acteurId, idNounou));
        Assert.Equal(idNounou, config.RoleDe(acteurId)); // pré-condition : Carla porte le rôle Nounou

        new RenommerRoleHandler(referentiel, referentiel).Handle(new RenommerRoleCommand(idNounou, "Assistante maternelle"));

        // Affectation COHÉRENTE : l'acteur pointe toujours le MÊME id de rôle (aucune recréation)...
        Assert.Equal(idNounou, config.RoleDe(acteurId));
        // ... et cet id résout désormais le nouveau libellé (le rôle affecté a bien été renommé, pas dupliqué).
        Assert.Equal("Assistante maternelle", referentiel.EnumererRoles().Single(r => r.Id == idNounou).Libelle);
    }
}
