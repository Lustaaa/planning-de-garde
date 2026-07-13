using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 37 — Sc.2 — Rejets sans écriture partielle + bornes s34/s36 inchangées (@back)
//   Étant donné un enfant déjà lié à un parent avec le rôle « père »
//   Quand la commande « lier » désigne un SECOND parent avec le rôle « père »
//   Alors le domaine REFUSE (pas deux « père »), sans aucune écriture (lien existant intact)
//   Et le même refus vaut pour deux « mère » sur un même enfant
//   Étant donné deux liens « parent-libre » sur un même enfant → ACCEPTÉS (répétable), borne 0..2 (s34)
//   Étant donné un acteur NON éligible (role-flag s36) → REFUSÉ quel que soit le rôle-du-lien demandé
//   Et dans tous les cas de refus le motif est restitué et le store reste INCHANGÉ
//
// Frontière Application : le handler LierEnfantParent vérifie l'invariant « pas deux liens de même rôle
// EXCLUSIF (père/mère) » AVANT toute écriture. « parent-libre » reste répétable (compat + neutralité).
public class Scenario37_S2_RejetsRoleDuLien
{
    private const string LeaId = "enfant-lea";

    private sealed class FoyerBuilder
    {
        public ConfigurationFoyerEnMemoire Config { get; } = new();
        public ReferentielRolesEnMemoire Roles { get; } = new();

        public FoyerBuilder()
        {
            Roles.Creer("role-papa", "Papa");
            Roles.MarquerParent("role-papa", true);
            Roles.Creer("role-nounou", "Nounou"); // NON marqué parent
        }

        public string Parent(string prenom)
        {
            var id = new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            Config.AffecterRole(id, "role-papa");
            return id;
        }

        public string NonEligible(string prenom)
        {
            var id = new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            Config.AffecterRole(id, "role-nounou");
            return id;
        }
    }

    private static LierEnfantParentHandler Handler(FoyerBuilder foyer, IEnumerationEnfants enfants, IEditeurEnfants referentiel)
        => new(enfants, foyer.Config, foyer.Roles, referentiel);

    // ---------- Acceptation — un SECOND « père » est refusé sans écriture (lien existant intact) ----------
    [Fact]
    public void Acceptation_Should_Refuser_un_second_pere_sans_ecriture_When_l_enfant_a_deja_un_pere()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var autre = foyer.Parent("Autre");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);
        handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.Pere));

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, autre, RoleDuLien.Pere));

        Assert.False(resultat.EstSucces);
        Assert.Equal("un père est déjà lié à cet enfant", resultat.Motif);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies); // aucune écriture partielle
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == papa).Role);
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == autre);
    }

    // ---------- Driver — deux « mère » refusé de la même façon ----------
    [Fact]
    public void Should_Refuser_une_seconde_mere_sans_ecriture_When_l_enfant_a_deja_une_mere()
    {
        var foyer = new FoyerBuilder();
        var maman = foyer.Parent("Maman");
        var autre = foyer.Parent("Autre");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);
        handler.Handle(new LierEnfantParentCommand(LeaId, maman, RoleDuLien.Mere));

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, autre, RoleDuLien.Mere));

        Assert.False(resultat.EstSucces);
        Assert.Equal("une mère est déjà liée à cet enfant", resultat.Motif);
        Assert.Single(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Driver — « parent-libre » RÉPÉTABLE (dans la borne 0..2) ----------
    [Fact]
    public void Should_Accepter_deux_parent_libre_When_dans_la_borne_0_2()
    {
        var foyer = new FoyerBuilder();
        var p1 = foyer.Parent("Un");
        var p2 = foyer.Parent("Deux");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        Assert.True(handler.Handle(new LierEnfantParentCommand(LeaId, p1, RoleDuLien.ParentLibre)).EstSucces);
        Assert.True(handler.Handle(new LierEnfantParentCommand(LeaId, p2, RoleDuLien.ParentLibre)).EstSucces);

        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal(2, lea.ParentsLies.Count);
        Assert.All(lea.ParentsLies, p => Assert.Equal(RoleDuLien.ParentLibre, p.Role));
    }

    // ---------- Driver — un acteur NON éligible (s36) est refusé quel que soit le rôle demandé ----------
    [Fact]
    public void Should_Refuser_un_acteur_non_eligible_sans_ecriture_When_le_role_du_lien_est_pere()
    {
        var foyer = new FoyerBuilder();
        var nounou = foyer.NonEligible("Nina");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);

        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, nounou, RoleDuLien.Pere));

        Assert.False(resultat.EstSucces);
        Assert.Equal("acteur sans rôle-parent", resultat.Motif);
        Assert.Empty(referentiel.EnumererEnfants().Single(e => e.Id == LeaId).ParentsLies);
    }

    // ---------- Driver — changer le rôle-du-lien de SON PROPRE parent vers « père » n'est PAS un conflit ----------
    [Fact]
    public void Should_Autoriser_le_changement_de_role_de_son_propre_parent_When_aucun_autre_parent_ne_porte_ce_role()
    {
        var foyer = new FoyerBuilder();
        var papa = foyer.Parent("Papa");
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        var handler = Handler(foyer, referentiel, referentiel);
        handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.Pere));

        // Ré-émettre « père » sur le même parent est neutre (idempotent), jamais un « deux pères ».
        var resultat = handler.Handle(new LierEnfantParentCommand(LeaId, papa, RoleDuLien.Pere));

        Assert.True(resultat.EstSucces);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies);
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single().Role);
    }
}
