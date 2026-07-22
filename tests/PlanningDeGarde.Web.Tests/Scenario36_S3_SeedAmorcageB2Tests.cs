using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 36 — Sc.3 — Amorçage B2 sur le COMPOSITION ROOT RÉEL (@back, InMemory — parité seed enfants/acteurs,
/// asymétrie s15 : jamais côté Mongo). Sur l'hôte réellement câblé (<see cref="ApiDistanteFactory"/>, store
/// réel), au seed du foyer les rôles Papa / Maman / Parent démarrent « est rôle parent » = true (pré-cochés),
/// Nounou / Grand-parent = false ; les acteurs-parents portent un rôle marqué parent (Alice → Papa, Bruno →
/// Maman) pour rester liables, les intervenants un rôle non-parent (Nina → Nounou, grand-père → Grand-parent),
/// Marie-Hélène (Admin) aucun rôle. Un rôle créé APRÈS le seed via <c>CreerRole</c> démarre non-parent, même
/// si son libellé est parent-ish — la source de vérité reste le flag posé explicitement (anti-piège s35).
/// </summary>
public sealed class Scenario36_S3_SeedAmorcageB2Tests
{
    private static bool EstParent(System.Collections.Generic.IReadOnlyCollection<PlanningDeGarde.Application.Foyer.Ports.RoleFoyer> roles, string libelle)
        => roles.Single(r => r.Libelle == libelle).EstRoleParent;

    [Fact]
    public void Au_seed_Papa_Maman_Parent_pre_coches_parent_Nounou_Grand_parent_non_et_acteurs_parents_lies_a_un_role_parent()
    {
        using var api = new ApiDistanteFactory();
        var roles = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles();
        var acteurs = api.Services.GetRequiredService<IEnumerationActeursFoyer>();

        // Rôles pré-cochés parent (source de vérité de l'éligibilité) : Papa / Maman / Parent = true.
        Assert.True(EstParent(roles, "Papa"));
        Assert.True(EstParent(roles, "Maman"));
        Assert.True(EstParent(roles, "Parent"));
        // Nounou / Grand-parent démarrent non-parent.
        Assert.False(EstParent(roles, "Nounou"));
        Assert.False(EstParent(roles, "Grand-parent"));

        // Affectations seed : acteurs-parents → rôle marqué parent ; intervenants → rôle non-parent.
        var idPapa = roles.Single(r => r.Libelle == "Papa").Id;
        var idMaman = roles.Single(r => r.Libelle == "Maman").Id;
        Assert.Equal(idPapa, acteurs.RoleDe("parent-a"));   // Alice → Papa (liable)
        Assert.Equal(idMaman, acteurs.RoleDe("parent-b"));  // Bruno → Maman (liable)
        Assert.Equal(roles.Single(r => r.Libelle == "Nounou").Id, acteurs.RoleDe("nounou"));           // Nina → Nounou (non liable)
        Assert.Equal(roles.Single(r => r.Libelle == "Grand-parent").Id, acteurs.RoleDe("grand-pere")); // grand-père → Grand-parent
        Assert.Null(acteurs.RoleDe("parent-c")); // Marie-Hélène (Admin) : aucun rôle → non liable
    }

    [Fact]
    public void Un_role_cree_apres_le_seed_demarre_non_parent_meme_avec_un_libelle_parent_ish()
    {
        using var api = new ApiDistanteFactory();
        var creer = api.Services.GetRequiredService<CreerRoleHandler>();

        // Un libellé parent-ish NON déjà semé (« Beau-papa ») : le pré-cochage ne vaut QUE pour le seed.
        var roleId = creer.Handle(new CreerRoleCommand("Beau-papa")).Valeur!.RoleId;

        var role = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles().Single(r => r.Id == roleId);
        Assert.False(role.EstRoleParent); // démarre non-parent (jamais reconnu au libellé, anti-piège s35)

        // Il ne devient parent QUE par une bascule explicite (Sc.2).
        Assert.True(api.Services.GetRequiredService<MarquerRoleParentHandler>()
            .Handle(new MarquerRoleParentCommand(roleId, true)).EstSucces);
        Assert.True(api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles()
            .Single(r => r.Id == roleId).EstRoleParent);
    }
}
