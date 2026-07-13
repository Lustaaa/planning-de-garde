using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 36 — Sc.1 — Éligibilité du lien basculée sur TypeActeur.Parent (@back, option A)
//   Étant donné un enfant déclaré dans le foyer (id stable + prénom, s30)
//   Et un acteur du foyer de type TypeActeur.Parent (Papa/Maman) NE portant AUCUN rôle nommé « Parent »
//   Quand la commande « lier un enfant à un parent » est émise (enfantId, acteurId)
//   Alors le lien enfant→parent est ACCEPTÉ et PERSISTÉ (relu par la query)
//   Et l'éligibilité « parent liable » est résolue sur TypeActeur.Parent — la MÊME source de vérité que
//      l'invariant admin=Parent (AdministrationFoyer.DesignerAdmin, s22)
//   Et le critère « rôle du référentiel de libellé littéral 'Parent' » n'intervient PLUS dans l'éligibilité
//
// Frontière Application : le handler LierEnfantParent résout l'éligibilité sur le TYPE de l'acteur
// (IEnumerationActeursFoyer.TypeDe), jamais sur le libellé de rôle du référentiel (s21).
public class Scenario36_S1_EligibiliteTypeActeur
{
    private const string LeaId = "enfant-lea";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Lier_un_acteur_de_type_Parent_sans_role_Parent_When_l_eligibilite_est_resolue_sur_le_type()
    {
        // Given — un acteur AJOUTÉ en session est de type Parent par défaut (Foyer.TypeParDefaut) et NE porte
        // AUCUN rôle ; un rôle « Parent » existe au référentiel mais N'EST PAS affecté à cet acteur.
        var roles = new ReferentielRolesEnMemoire();
        new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand("Parent"));
        var config = new ConfigurationFoyerEnMemoire();
        var papaId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Papa")).Valeur!.ActeurId;

        // Précondition explicite : l'acteur EST de type Parent (même prédicat que l'invariant admin=Parent)
        // ET ne porte aucun rôle (le libellé « Parent » du référentiel ne le qualifie pas).
        Assert.Equal(TypeActeur.Parent, config.TypeDe(papaId));
        Assert.Null(config.RoleDe(papaId));

        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = new LierEnfantParentHandler(referentiel, config, referentiel);

        // When — la commande « lier » désigne cet acteur.
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papaId));

        // Then — le lien est ACCEPTÉ et PERSISTÉ (relu par la query), sans qu'aucun rôle « Parent » n'ait été créé/affecté.
        Assert.True(resultat.EstSucces);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Contains(papaId, lea.ParentsLies);
    }
}
