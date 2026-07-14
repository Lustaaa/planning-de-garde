using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 40 — Sc.3 — AUCUN blocage d'écriture : R3 signalée, jamais imposée (@back)
//   Étant donné un enfant sans parent, ou avec un seul parent, ou avec deux « parent-libre »
//   Quand je lie / délie / enregistre via le canal d'écriture existant (LierEnfantParent, délier)
//   Alors l'écriture RÉUSSIT dans tous les cas (0, 1 ou 2 parents acceptés, comme s34)
//   Et AUCUN nouvel invariant « exactement 2 parents » n'est imposé à la pose ni à l'enregistrement
//   Et le calcul du statut de complétude ne modifie, ne refuse et ne déclenche AUCUNE écriture (lecture pure)
//
// GUARD de non-régression (le statut R3 est SIGNALÉ, pas IMPOSÉ) : sur les adaptateurs InMemory RÉELS, les
// handlers d'écriture s34 continuent d'accepter 0/1/2 parents SANS nouvelle contrainte, et exécuter la
// projection enrichie (GrapheFoyerQuery s40) ne mute jamais le store. Acceptation Mongo : fichier Api.Tests.
public class Scenario40_S3_AucunBlocageEcriture
{
    private const string EnfantId = "enfant-lea";

    private sealed class Foyer
    {
        public ConfigurationFoyerEnMemoire Config { get; } = new();
        public ReferentielRolesEnMemoire Roles { get; } = new();
        public ReferentielEnfantsEnMemoire Enfants { get; } = new();

        public Foyer()
        {
            Roles.Creer("role-parent", "Parent");
            Roles.MarquerParent("role-parent", true);
            Enfants.Ajouter(EnfantId, "Léa");
        }

        public string Parent(string prenom)
        {
            var id = new AjouterActeurHandler(Config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
            Config.AffecterRole(id, "role-parent");
            return id;
        }

        public LierEnfantParentHandler Lier() => new(Enfants, Config, Roles, Enfants);
        public DelierEnfantParentHandler Delier() => new(Enfants);
        public StatutCoupleR3 Statut()
            => new GrapheFoyerQuery(Enfants, Config, Config).Lire().Single(e => e.EnfantId == EnfantId).StatutCouple;
    }

    [Fact]
    public void Should_Accepter_l_enregistrement_a_un_seul_parent_sans_imposer_deux()
    {
        // Un seul parent lié (incomplet R3) : l'écriture RÉUSSIT — aucune contrainte « exactement 2 » ajoutée.
        var foyer = new Foyer();
        var papa = foyer.Parent("Papa");

        var resultat = foyer.Lier().Handle(new LierEnfantParentCommand(EnfantId, papa, RoleDuLien.Pere));

        Assert.True(resultat.EstSucces);
        Assert.Single(foyer.Enfants.EnumererEnfants().Single(e => e.Id == EnfantId).ParentsLies);
        // Le statut le SIGNALE (incomplet) sans jamais avoir refusé l'écriture.
        Assert.Equal(StatutCoupleR3.Incomplet, foyer.Statut());
    }

    [Fact]
    public void Should_Accepter_deux_parents_libres_sans_exiger_le_couple_pere_mere()
    {
        // Deux « parent-libre » (2 parents mais pas le couple père+mère) : les DEUX écritures réussissent.
        var foyer = new Foyer();
        var a = foyer.Parent("A");
        var b = foyer.Parent("B");

        Assert.True(foyer.Lier().Handle(new LierEnfantParentCommand(EnfantId, a, RoleDuLien.ParentLibre)).EstSucces);
        Assert.True(foyer.Lier().Handle(new LierEnfantParentCommand(EnfantId, b, RoleDuLien.ParentLibre)).EstSucces);

        Assert.Equal(2, foyer.Enfants.EnumererEnfants().Single(e => e.Id == EnfantId).ParentsLies.Count);
        Assert.Equal(StatutCoupleR3.Incomplet, foyer.Statut()); // signalé incomplet, jamais bloqué
    }

    [Fact]
    public void Should_Accepter_de_delier_jusqu_a_zero_parent_sans_blocage()
    {
        // Délier jusqu'à 0 parent (racine isolée légitime) : le retrait RÉUSSIT, aucun invariant « min 1/2 ».
        var foyer = new Foyer();
        var papa = foyer.Parent("Papa");
        foyer.Lier().Handle(new LierEnfantParentCommand(EnfantId, papa, RoleDuLien.Pere));

        var resultat = foyer.Delier().Handle(new DelierEnfantParentCommand(EnfantId, papa));

        Assert.True(resultat.EstSucces);
        Assert.Empty(foyer.Enfants.EnumererEnfants().Single(e => e.Id == EnfantId).ParentsLies);
        Assert.Equal(StatutCoupleR3.Vide, foyer.Statut()); // état neutre, pas une anomalie bloquante
    }

    [Fact]
    public void Should_Ne_declencher_aucune_ecriture_When_le_statut_est_calcule()
    {
        // Le calcul du statut est un chemin de LECTURE PUR : exécuter la projection enrichie ne mute pas le store.
        var foyer = new Foyer();
        var papa = foyer.Parent("Papa");
        foyer.Lier().Handle(new LierEnfantParentCommand(EnfantId, papa, RoleDuLien.Pere));

        var avant = foyer.Enfants.EnumererEnfants().Single(e => e.Id == EnfantId).ParentsLies
            .Select(p => p.ActeurId).OrderBy(x => x).ToList();

        // Deux exécutions successives de la projection enrichie (dont le calcul du statut).
        _ = foyer.Statut();
        _ = foyer.Statut();

        var apres = foyer.Enfants.EnumererEnfants().Single(e => e.Id == EnfantId).ParentsLies
            .Select(p => p.ActeurId).OrderBy(x => x).ToList();
        Assert.Equal(avant, apres); // store INCHANGÉ — aucune écriture déclenchée par la lecture du statut
    }
}
