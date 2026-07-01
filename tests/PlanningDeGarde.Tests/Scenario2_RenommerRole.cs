using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.2 — Renommer un rôle du référentiel (@back)
//   Tranche BACKEND (frontière Application) : commande/handler RenommerRole qui mute le libellé d'un
//   rôle SUR SON IDENTIFIANT STABLE (jamais éditable) via le port d'écriture IEditeurReferentielRoles,
//   sans créer de doublon (le même id reste un unique rôle). La durabilité sur store Mongo réel (relu
//   après redémarrage) est prouvée séparément en Api.Tests. On NE teste PAS ici de rendu Blazor.
public class Scenario2_RenommerRole
{
    private const string Nounou = "Nounou";
    private const string AssistanteMaternelle = "Assistante maternelle";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin à la frontière Application : un rôle « Nounou » existe (id connu) ;
    // le parent le renomme « Assistante maternelle » ; le référentiel doit conserver le MÊME id, porter
    // le nouveau libellé, et rester un UNIQUE rôle pour cet id (aucun doublon).
    [Fact]
    public void Acceptation_Should_Conserver_le_meme_id_stable_avec_le_nouveau_libelle_et_aucun_doublon_When_le_parent_renomme_Nounou_en_Assistante_maternelle()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        var idNounou = new CreerRoleHandler(referentiel).Handle(new CreerRoleCommand(Nounou)).Valeur!.RoleId;
        var handler = new RenommerRoleHandler(referentiel);

        var resultat = handler.Handle(new RenommerRoleCommand(idNounou, AssistanteMaternelle));

        Assert.True(resultat.EstSucces);
        Assert.Equal(idNounou, resultat.Valeur!.RoleId);                 // id stable inchangé
        var roles = referentiel.EnumererRoles();
        Assert.Single(roles);                                            // toujours un seul rôle (aucun doublon)
        Assert.Equal(idNounou, roles.Single().Id);                       // ... porté par le MÊME id
        Assert.Equal(AssistanteMaternelle, roles.Single().Libelle);      // ... avec le nouveau libellé
    }

    // ---------- Test #1 — Driver : le renommage applique le nouveau libellé sur le même id ----------
    // Contradiction : aucune commande/handler RenommerRole n'existe — le référentiel ne sait que créer.
    // Force l'orchestration : le renommage mute le libellé du rôle SUR SON IDENTIFIANT STABLE via le
    // port d'écriture, de sorte que le rôle soit énuméré avec le nouveau libellé sur le même id.
    [Fact]
    public void Should_Appliquer_le_nouveau_libelle_sur_le_meme_identifiant_stable_When_le_parent_renomme_un_role()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-nounou", Nounou);
        var handler = new RenommerRoleHandler(referentiel);

        handler.Handle(new RenommerRoleCommand("role-nounou", AssistanteMaternelle));

        var role = referentiel.EnumererRoles().Single(r => r.Id == "role-nounou");
        Assert.Equal(AssistanteMaternelle, role.Libelle); // nouveau libellé résolu sur le même id
    }

    // ---------- Test #2 — Driver : le renommage ne crée pas de doublon ----------
    // Contradiction : une impl qui « ajouterait » un rôle au lieu de muter l'existant créerait un second
    // rôle. Force une mutation en place : le référentiel ne contient toujours qu'UN rôle pour cet id.
    [Fact]
    public void Should_Ne_pas_creer_de_doublon_le_referentiel_reste_a_un_seul_role_When_un_role_est_renomme()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-nounou", Nounou);
        var handler = new RenommerRoleHandler(referentiel);

        handler.Handle(new RenommerRoleCommand("role-nounou", AssistanteMaternelle));

        Assert.Single(referentiel.EnumererRoles()); // aucun doublon : toujours un seul rôle
    }
}
