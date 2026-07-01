using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.1 — Créer un rôle dans le référentiel (@back)
//   Tranche BACKEND (frontière Application) : commande/handler CreerRole qui génère un IDENTIFIANT
//   STABLE NEUF OPAQUE (jamais dérivé du libellé), persiste le rôle via le port d'écriture
//   IEditeurReferentielRoles, et l'expose à l'énumération IEnumerationRoles EXACTEMENT UNE FOIS.
//   La durabilité sur store Mongo réel (survit au redémarrage) est prouvée séparément en Api.Tests
//   (acceptation runtime obligatoire). On NE teste PAS ici de rendu Blazor.
public class Scenario1_CreerRole
{
    private const string Nounou = "Nounou";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin à la frontière Application (sans IHM) : un parent crée le rôle
    // « Nounou » ; le store réel (référentiel de rôles) doit ensuite l'ÉNUMÉRER EXACTEMENT UNE FOIS,
    // porté par un identifiant NEUF OPAQUE, distinct du libellé et non dérivé de lui.
    [Fact]
    public void Acceptation_Should_Enumerer_Nounou_exactement_une_fois_sur_un_identifiant_neuf_opaque_non_derive_du_libelle_When_le_parent_cree_le_role_Nounou()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        var handler = new CreerRoleHandler(referentiel);

        var resultat = handler.Handle(new CreerRoleCommand(Nounou));

        Assert.True(resultat.EstSucces);
        var idNounou = resultat.Valeur!.RoleId;
        var roles = referentiel.EnumererRoles();
        Assert.Single(roles, r => r.Libelle == Nounou);       // « Nounou » énuméré EXACTEMENT une fois
        var role = roles.Single(r => r.Libelle == Nounou);
        Assert.Equal(idNounou, role.Id);                      // ... porté par l'id neuf retourné
        Assert.NotEqual(Nounou, idNounou);                    // ... identifiant opaque, jamais le libellé
    }

    // ---------- Test #1 — Driver : une création fait EXISTER le rôle, résolu par son libellé sur un id neuf ----------
    // Contradiction : aucune commande/handler CreerRole n'existe — le référentiel de rôles n'a pas de
    // chemin d'écriture. Force l'orchestration : une création génère un identifiant et persiste le
    // libellé via le port d'écriture, de sorte que le rôle soit énuméré avec libellé « Nounou » sur l'id.
    [Fact]
    public void Should_Faire_exister_le_role_cree_resolu_par_son_libelle_sur_un_identifiant_neuf_When_le_parent_cree_un_role()
    {
        var referentiel = new FakeReferentielRoles();
        var handler = new CreerRoleHandler(referentiel);

        var resultat = handler.Handle(new CreerRoleCommand(Nounou));

        Assert.True(resultat.EstSucces);
        var idNeuf = resultat.Valeur!.RoleId;
        var role = referentiel.EnumererRoles().Single(r => r.Id == idNeuf);
        Assert.Equal(Nounou, role.Libelle); // le rôle créé est résolu par son libellé sur l'id généré
    }

    // ---------- Test #2 — Driver : l'identifiant est OPAQUE, distinct du libellé ----------
    // Contradiction : l'impl minimale du #1 pourrait prendre le raccourci « id = libellé »
    // (libellé-comme-identité, anti-pattern corrigé au s06). Force un identifiant OPAQUE généré, ≠ libellé.
    [Fact]
    public void Should_Porter_un_identifiant_opaque_distinct_du_libelle_When_un_role_est_cree()
    {
        var referentiel = new FakeReferentielRoles();
        var handler = new CreerRoleHandler(referentiel);

        var idNeuf = handler.Handle(new CreerRoleCommand(Nounou)).Valeur!.RoleId;

        Assert.NotEqual(Nounou, idNeuf); // identifiant opaque, jamais le libellé (anti-pattern s06)
    }
}
