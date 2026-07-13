using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 36 — Sc.4 — Éligibilité du lien basculée sur « rôle marqué parent » (REMPLACE l'option A) @back
//   Étant donné un enfant déclaré dans le foyer (s30)
//   Et un acteur portant un rôle dont « est rôle parent » = true (ex. Alice → Papa)
//   Quand la commande « lier » est émise → le lien est ACCEPTÉ et PERSISTÉ (relu par la query)
//   Et l'éligibilité est résolue sur « l'acteur porte un rôle marqué parent » — TypeActeur.Parent n'intervient PLUS
//   Étant donné un acteur portant un rôle « est rôle parent » = false (Nounou/Grand-parent) → REFUSÉ, sans écriture
//   Étant donné un acteur SANS aucun rôle affecté (RoleId = null) → REFUSÉ (aucun rôle marqué parent porté)
//
// Frontière Application : le handler résout l'éligibilité sur le FLAG du rôle porté (source de vérité unique,
// jamais le libellé ni le TypeActeur) ; toutes les gardes AVANT toute écriture (aucune écriture partielle).
public class Scenario36_S4_EligibiliteRoleParent
{
    private const string LeaId = "enfant-lea";

    private static ReferentielRolesEnMemoire ReferentielRoles()
    {
        var roles = new ReferentielRolesEnMemoire();
        roles.Creer("role-papa", "Papa");
        roles.MarquerParent("role-papa", true);   // rôle marqué parent
        roles.Creer("role-nounou", "Nounou");     // rôle NON marqué parent (défaut neutre)
        return roles;
    }

    // ---------- Acceptation — un acteur à rôle marqué parent est ACCEPTÉ (l'éligibilité suit le flag) ----------
    [Fact]
    public void Acceptation_Should_Lier_un_acteur_portant_un_role_marque_parent_When_l_eligibilite_suit_le_flag()
    {
        // Given — un acteur ajouté (TypeActeur.Parent par défaut, NON pertinent ici) affecté au rôle « Papa »
        // marqué parent. Le type ne qualifie plus : c'est le flag du rôle qui rend liable.
        var roles = ReferentielRoles();
        var config = new ConfigurationFoyerEnMemoire();
        var papaId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        config.AffecterRole(papaId, "role-papa");

        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = new LierEnfantParentHandler(referentiel, config, roles, referentiel);

        // When — la commande « lier » désigne cet acteur.
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papaId));

        // Then — le lien est ACCEPTÉ et PERSISTÉ (relu par la query).
        Assert.True(resultat.EstSucces);
        Assert.Contains(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies, p => p.ActeurId == papaId);
    }

    // ---------- Driver — un acteur à rôle NON marqué parent est REFUSÉ, sans écriture partielle ----------
    [Fact]
    public void Should_Refuser_un_acteur_a_role_non_marque_parent_sans_ecriture_When_le_flag_est_false()
    {
        // Given — un acteur affecté au rôle « Nounou » (non marqué parent) ; un lien préexistant pour prouver
        // l'absence d'écriture partielle.
        var roles = ReferentielRoles();
        var config = new ConfigurationFoyerEnMemoire();
        var nounouId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Nina")).Valeur!.ActeurId;
        config.AffecterRole(nounouId, "role-nounou");
        var papaId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        config.AffecterRole(papaId, "role-papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, papaId);
        var handler = new LierEnfantParentHandler(referentiel, config, roles, referentiel);

        // When — on tente de lier l'acteur à rôle non-parent.
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, nounouId));

        // Then — REFUS, motif restitué, lien préexistant intact (aucune écriture partielle).
        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur sans rôle-parent", resultat.Motif);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies);
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == nounouId);
    }

    // ---------- Driver — un acteur SANS aucun rôle est REFUSÉ (aucun rôle marqué parent porté) ----------
    [Fact]
    public void Should_Refuser_un_acteur_sans_aucun_role_sans_ecriture_When_le_roleId_est_null()
    {
        var roles = ReferentielRoles();
        var config = new ConfigurationFoyerEnMemoire();
        var sansRoleId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Marie")).Valeur!.ActeurId;
        Assert.Null(config.RoleDe(sansRoleId)); // aucun rôle affecté
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = new LierEnfantParentHandler(referentiel, config, roles, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, sansRoleId));

        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur sans rôle-parent", resultat.Motif);
        Assert.Empty(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Driver — borne 0..2 parents (s34) inchangée : un 3ᵉ parent refusé ----------
    [Fact]
    public void Should_Refuser_un_3e_parent_marque_When_la_borne_0_2_reste_tenue()
    {
        var roles = ReferentielRoles();
        var config = new ConfigurationFoyerEnMemoire();
        string Parent(string prenom)
        {
            var id = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            config.AffecterRole(id, "role-papa");
            return id;
        }
        var p1 = Parent("Alice");
        var p2 = Parent("Bruno");
        var p3 = Parent("Chloé");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, p1);
        referentiel.LierParent(LeaId, p2);
        var handler = new LierEnfantParentHandler(referentiel, config, roles, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, p3));

        Assert.False(resultat.EstSucces);
        Assert.Equal("2 parents max", resultat.Motif);
        Assert.Equal(2, referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies.Count);
    }
}
