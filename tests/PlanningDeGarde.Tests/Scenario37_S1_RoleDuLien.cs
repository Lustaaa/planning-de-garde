using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 37 — Sc.1 — Lien enfant↔parent enrichi d'un « rôle-du-lien » + « lier » avec rôle (@back)
//   Étant donné un enfant lié à un parent-acteur (lien s34, éligibilité role-flag s36)
//   Quand la commande « lier » est émise avec un rôle-du-lien (enfantId, acteurId, rôle ∈ {père, mère, parent-libre})
//   Alors le lien enfant→parent porte le rôle-du-lien, relu par la query pour chaque parent lié
//   Et l'id stable de l'enfant reste inchangé (enrichissement additif, pas recréation)
//   Et lier SANS préciser de rôle vaut « parent-libre » (défaut neutre, comportement s34 préservé)
//   Et modifier le rôle-du-lien d'un parent déjà lié met à jour le lien SANS le dupliquer
//
// Frontière Application : le handler LierEnfantParent porte le rôle-du-lien jusqu'au store, relu par
// la query IEnumerationEnfants dans EnfantFoyer.ParentsLies (collection de ParentLie {ActeurId, Rôle}).
public class Scenario37_S1_RoleDuLien
{
    private const string LeaId = "enfant-lea";

    private sealed class FoyerBuilder
    {
        public ConfigurationFoyerEnMemoire Config { get; } = new();
        public ReferentielRolesEnMemoire Roles { get; } = new();

        public FoyerBuilder()
        {
            Roles.Creer("role-papa", "Papa");
            Roles.MarquerParent("role-papa", true); // rôle marqué parent → éligibilité (s36)
        }

        public string Parent(string prenom)
        {
            var id = new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            Config.AffecterRole(id, "role-papa");
            return id;
        }
    }

    private static LierEnfantParentHandler Handler(FoyerBuilder foyer, IEnumerationEnfants enfants, IEditeurEnfants referentiel)
        => new(enfants, foyer.Config, foyer.Roles, referentiel);

    // ---------- Acceptation — lier avec un rôle « père » : la query relit le parent AVEC son rôle-du-lien ----------
    [Fact]
    public void Acceptation_Should_Relire_le_parent_avec_son_role_du_lien_When_on_lie_avec_un_role_pere()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.Pere));

        Assert.True(resultat.EstSucces);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal(LeaId, lea.Id); // id stable inchangé (enrichissement additif)
        var lien = Assert.Single(lea.ParentsLies);
        Assert.Equal(papa, lien.ActeurId);
        Assert.Equal(RoleDuLien.Pere, lien.Role);
    }

    // ---------- Driver — lier SANS rôle vaut « parent-libre » (défaut neutre, comportement s34 préservé) ----------
    [Fact]
    public void Should_Valoir_parent_libre_When_on_lie_sans_preciser_de_role()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        handler.Handle(new LierEnfantParentCommand(LeaId, papa)); // aucun rôle explicite

        var lien = Assert.Single(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
        Assert.Equal(RoleDuLien.ParentLibre, lien.Role);
    }

    // ---------- Driver — modifier le rôle-du-lien d'un parent déjà lié met à jour SANS dupliquer ----------
    [Fact]
    public void Should_Mettre_a_jour_le_role_sans_dupliquer_When_le_parent_est_deja_lie()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var maman = foyer.Parent("Maman");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);
        handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.ParentLibre));
        handler.Handle(new LierEnfantParentCommand(LeaId, maman, RoleDuLien.Mere));

        // On ré-émet « lier » sur papa avec un NOUVEAU rôle « père »
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.Pere));

        Assert.True(resultat.EstSucces);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal(2, lea.ParentsLies.Count); // pas de doublon (id enfant + autres liens intacts)
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == papa).Role); // rôle mis à jour
        Assert.Equal(RoleDuLien.Mere, lea.ParentsLies.Single(p => p.ActeurId == maman).Role); // autre lien intact
    }
}
