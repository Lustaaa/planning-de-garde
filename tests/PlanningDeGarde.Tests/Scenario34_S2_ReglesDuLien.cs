using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 34 — S2 — Règles du lien : « 2 parents max » + rejets, sans écriture partielle (@back)
//   Étant donné un enfant déjà lié à DEUX parents
//   Quand la commande « lier » émet un TROISIÈME parent
//   Alors le domaine REFUSE (borne « 2 parents max »), sans aucune écriture (les 2 liens intacts)
//   Étant donné une commande « lier » désignant un acteur qui N'EXISTE PAS → REFUS (inexistant), sans écriture
//   Étant donné un acteur existant mais NON Parent → REFUS (non-parent), sans écriture
//   Étant donné un parent DÉJÀ lié → opération NEUTRE (pas de doublon), sans écriture partielle
//   Et dans tous les cas de refus, le motif est restitué et le store reste INCHANGÉ
//
// Frontière Application : le handler LierEnfantParent vérifie TOUTES les règles AVANT toute écriture.
// « Parent » = acteur portant le rôle « Parent » du référentiel de rôles (cadrage SM).
public class Scenario34_S2_ReglesDuLien
{
    private const string LeaId = "enfant-lea";

    private sealed class FoyerBuilder
    {
        public ReferentielRolesEnMemoire Roles { get; } = new();
        public ConfigurationFoyerEnMemoire Config { get; } = new();
        private readonly string _roleParent;

        public FoyerBuilder()
        {
            _roleParent = new CreerRoleHandler(Roles, Roles).Handle(new CreerRoleCommand("Parent")).Valeur!.RoleId;
        }

        /// <summary>Acteur portant le rôle « Parent » — candidat valide au lien.</summary>
        public string Parent(string prenom)
        {
            var id = new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            new AffecterRoleActeurHandler(Roles, Config).Handle(new AffecterRoleActeurCommand(id, _roleParent));
            return id;
        }

        /// <summary>Acteur existant SANS rôle « Parent » — non liable.</summary>
        public string NonParent(string prenom)
            => new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
    }

    private static LierEnfantParentHandler Handler(FoyerBuilder foyer, IEnumerationEnfants enfants, IEditeurEnfants referentiel)
        => new(enfants, foyer.Config, foyer.Roles, referentiel);

    // ---------- Acceptation — « 2 parents max » : un 3ᵉ parent est refusé sans écriture ----------
    [Fact]
    public void Acceptation_Should_Refuser_un_troisieme_parent_sans_ecriture_When_l_enfant_a_deja_deux_parents()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var maman = foyer.Parent("Maman");
        var mamie = foyer.Parent("Mamie");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, papa);
        referentiel.LierParent(LeaId, maman);
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, mamie));

        Assert.False(resultat.EstSucces);
        Assert.Equal("2 parents max", resultat.Motif);

        // Les 2 liens existants restent intacts, le 3ᵉ absent (aucune écriture partielle)
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal(2, lea.ParentsLies.Count);
        Assert.Contains(papa, lea.ParentsLies);
        Assert.Contains(maman, lea.ParentsLies);
        Assert.DoesNotContain(mamie, lea.ParentsLies);
    }

    // ---------- Driver — acteur inexistant : refus sans écriture ----------
    [Fact]
    public void Should_Refuser_un_acteur_inexistant_sans_ecriture_When_l_acteur_est_absent_du_referentiel()
    {
        var foyer = new FoyerBuilder();
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, "acteur-fantome"));

        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur inexistant", resultat.Motif);
        Assert.Empty(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Driver — acteur existant mais non Parent : refus sans écriture ----------
    [Fact]
    public void Should_Refuser_un_acteur_non_parent_sans_ecriture_When_l_acteur_ne_porte_pas_le_role_Parent()
    {
        var foyer = new FoyerBuilder();
        var bob = foyer.NonParent("Bob"); // existe mais sans rôle Parent
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, bob));

        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur non parent", resultat.Motif);
        Assert.Empty(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Driver — parent déjà lié : opération neutre, pas de doublon ----------
    [Fact]
    public void Should_Etre_neutre_sans_doublon_When_le_parent_est_deja_lie()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, papa);
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papa));

        // Neutre : pas d'échec, et surtout AUCUN doublon de lien
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies);
        Assert.Contains(papa, lea.ParentsLies);
    }
}
